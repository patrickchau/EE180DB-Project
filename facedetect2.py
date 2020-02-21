from __future__ import print_function

import numpy as np
import cv2 as cv
import math
import socket
import os
import string
import re
import sys
import time
import getopt

# local modules
from video import create_capture
from common import clock, draw_str

#TODO: dynamically size the rectangle based on face to pass to unity,
#      intelligently decide which face is which

# UDP Connection Information
UDP_IP = '127.0.0.1'
UDP_PORT = 5065
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
abs_path = os.path.dirname(__file__)                       # local path
rel_path = 'img.png'

def detect(img, cascade): # detects all the faces and assigns bounding boxes to them
    rects = cascade.detectMultiScale(img, scaleFactor=1.1, minNeighbors=4, minSize=(30, 30),
                                     flags=cv.CASCADE_SCALE_IMAGE)
    if len(rects) == 0:
        return []
    rects[:,2:] += rects[:,:2]
    return rects

def draw_rects(img, rects, color):
    for x1, y1, x2, y2 in rects:
        cv.rectangle(img, (x1, y1), (x2, y2), color, 2)

def main():
    args, video_src = getopt.getopt(sys.argv[1:], '', ['cascade=', 'nested-cascade='])
    try:
        video_src = video_src[0]
    except:
        video_src = 0
    args = dict(args)

    # pull in the haar cascade classifiers
    # see: https://docs.opencv.org/3.4/db/d28/tutorial_cascade_classifier.html
    cascade_fn = args.get('--cascade', "haarcascades/haarcascade_frontalface_alt.xml")
    nested_fn  = args.get('--nested-cascade', "haarcascades/haarcascade_eye.xml")

    cascade = cv.CascadeClassifier(cv.samples.findFile(cascade_fn))
    nested = cv.CascadeClassifier(cv.samples.findFile(nested_fn))

    # 'convenience function for capture creation'
    cam = create_capture(video_src, fallback='synth:bg={}:noise=0.05'.format(cv.samples.findFile('lena.jpg')))

    _ret, img = cam.read()
    gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
    gray = cv.equalizeHist(gray)    
    rects = detect(gray, cascade) # the large bounding boxes around faces
    print("This is the initial reading: ")
    print(rects)

    face_rects = rects.copy()
    # continuously check the camera and update the bounding boxes
    while True:
        # read from the camera and turn into grayscale
        _ret, img = cam.read()
        gray = cv.cvtColor(img.copy(), cv.COLOR_BGR2GRAY)
        gray = cv.equalizeHist(gray)

        t = clock()
        rects = detect(gray, cascade) # the large bounding boxes around faces
        print(rects)

        # update face_rects based on new values
        # problem is that rects occurs in a random order
        # so associate position w/ least norm
        death = rects.copy() # this is already a numpy array

        """
        doesnt work lol
        # if a new face appears, 
        # itll be the furthest off from what's already there
        # therefore for each entry in the updated faces, 
        # see which has the least norm and exclude that one
        choice = 0
        c = 0
        for cur in rects:
            cur_np = np.array(cur)
            count = 0
            least = 10000
            for comp in face_rects:
                comp_np = np.array(comp)
                if( np.linalg.norm(cur_np-comp_np) < least ): # new least norm
                    choice = count
                count += 1
            death = np.delete(death, choice)
            if c < len(face_rects):
                face_rects[c] = rects[choice]
            c+=1
        # if there's an extra face, just append it
        if (len(rects) == len(face_rects)+1):
            #face_rects.append(death[0])
            print("this is face")
            print(face_rects)
            print("this is death")
            print(death)
            face_rects = np.concatenate(face_rects, death[0])
        
        print(face_rects)
        """

        # draws the rectangles that make up the bounding box
        vis = img.copy()
        draw_rects(vis, rects, (0, 255, 0))
        if not nested.empty():
            for x1, y1, x2, y2 in rects:
                roi = gray[y1:y2, x1:x2]
                vis_roi = vis[y1:y2, x1:x2]
                subrects = detect(roi.copy(), nested)
                draw_rects(vis_roi, subrects, (255, 0, 0))
        dt = clock() - t

        draw_str(vis, (20, 20), 'time: %.1f ms' % (dt*1000))
        cv.imshow('facedetect', vis)

        # currently set to only display the image of the person on the left hand side of screen
        if (len(rects) > 0):    # if rects is not empty, update on unity end
            x_marg = 0 
            y_marg = 0
            kop = 0
            x1 = 0
            x2 = 0
            y1 = 0
            y2 = 0
            smallest = 25000
            for update in rects:
                co = '' # x1 y1 x2 y2
                flag = False
                count = 0
                for coord in update:
                    co += str(coord) + ' '
                    if not flag and update[0] < smallest:
                        smallest = update[0]
                        flag = True
                    if count == 0 and flag:
                        x1 = coord
                    elif count == 1 and flag:
                        y1 = coord
                    elif count == 2 and flag:
                        x2 = coord
                    else:
                        if flag:
                            y2 = coord
                    count += 1
                if flag:
                    x_marg = x2 - x1
                    y_marg = y2 - y1
                sock.sendto( (co).encode(), (UDP_IP, UDP_PORT) )
            # save the cropped image for unity to pull
            crop_img = img[y1:y1+y_marg, x1:x1+x_marg].copy()
            cv.imwrite('savedImage.png', crop_img)
            sock.sendto( 'updated'.encode(), (UDP_IP, UDP_PORT) )

        # exit out of program if pressing escape
        if cv.waitKey(5) == 27:
            break

    print('Done')


if __name__ == '__main__':
    print(__doc__)
    main()
    cv.destroyAllWindows() 
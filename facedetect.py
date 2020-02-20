#!/usr/bin/env python

'''
face detection using haar cascades
USAGE:
    facedetect.py [--cascade <cascade_fn>] [--nested-cascade <cascade_fn>] [<video_source>]
'''
# code from opencv sample
# Python 2/3 compatibility
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
import color_detection as cd

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

# detects all the faces and assigns bounding boxes to them
def detect(img, cascade): 
    rects = cascade.detectMultiScale(img, scaleFactor=1.1, minNeighbors=4, minSize=(30, 30),
                                     flags=cv.CASCADE_SCALE_IMAGE)
    if len(rects) == 0:
        return []
    rects[:,2:] += rects[:,:2]
    return rects

# given a set of coords, draw the rectangles onto the image
def draw_rects(img, rects, color):
    for x1, y1, x2, y2 in rects:
        cv.rectangle(img, (x1, y1), (x2, y2), color, 2)

def find_rects(img, cascade):
    gray = cv.cvtColor(img.copy(), cv.COLOR_BGR2GRAY)
    gray = cv.equalizeHist(gray)
    rects = detect(gray, cascade) # the large bounding boxes around faces
    print (rects)
    return rects

def main():
    # first see if there is access to a webcam to use as source
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

    # files provided from OpenCV sample file for face detection
    cascade = cv.CascadeClassifier(cv.samples.findFile(cascade_fn))
    nested = cv.CascadeClassifier(cv.samples.findFile(nested_fn))

    # 'convenience function for capture creation'
    cam = create_capture(video_src, fallback='synth:bg={}:noise=0.05'.format(cv.samples.findFile('lena.jpg')))

    _ret, img = cam.read()
    # rects = detect(gray, cascade) # the large bounding boxes around faces

    # continuously check the camera and update the bounding boxes
    while True:
        # read from the camera and turn into grayscale
        _ret, img = cam.read()
        # clock for measuring time in between frames
        
        # find the bounding boxes for faces
        rects = find_rects(img, cascade)

        #gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        #gray = cv.equalizeHist(gray)
        t = clock() 
        #rects = detect(gray, cascade) # the large bounding boxes around faces

        # draws the rectangles that make up the bounding box
        # vis is the copy of the raw image that we will use for processing, so as to not mess up raw image
        vis = img.copy()
        draw_rects(vis, rects, (0, 255, 0))

        # find_color_point will return an image with all other colors except desired color masked out
        # will also have the image of the circle on it
        # see color_detection.py for source code
        # out = cd.find_color_point(1, vis)
        
        # adds the timestamp in top left
        dt = clock() - t
        draw_str(vis, (20, 20), 'time: %.1f ms' % (dt*1000))

        # draw the image after all has been put onto it
        cv.imshow('facedetect', vis)

        # goal here is to get the circle and the raw image 
        # so we need to pass the center and the image on the raw image
        # currently set to only save the image of the person on the left hand side of screen
        if (len(rects) > 0):    # if rects is not empty, update on unity end
            origin = (0,0) # the point from which distances are calc'd
            (x_marg, y_marg) = (0,0)
            (x1,x2,y1,y2) = (0,0,0,0)
            smallest = 25000
            for update in rects:
                co = '' # x1 y1 x2 y2
                # for the given origin, find the smallest norm from (x1,y1) of the rect
                if math.sqrt( math.pow((origin[0] - update[0]),2) + math.pow((origin[1] - update[1]),2)  )  < smallest:
                    # if a new smallest found, then update the coords
                    count = 0
                    for coord in update:
                        co += str(coord)  
                        if count == 0:
                            x1 = coord
                        elif count == 1:
                            y1 = coord
                        elif count == 2:
                            x2 = coord
                        elif count == 3:
                            y2 = coord
                        count += 1
                    x_marg = x2 - x1
                    y_marg = y2 - y1
                # sock.sendto( (co).encode(), (UDP_IP, UDP_PORT) )
            # save the cropped image for unity to pull
            crop_img = img[y1:y1+y_marg, x1:x1+x_marg].copy()

            cv.imwrite('fullimage.jpg', img)        # saves raw image 
            cv.imwrite('savedImage.jpg', crop_img)  # saves face image for unity
            sock.sendto( 'updated'.encode(), (UDP_IP, UDP_PORT) )   # sends the coords for face

        # exit out of program if pressing escape
        if cv.waitKey(5) == 27:
            break
    print('Done')


if __name__ == '__main__':
    print(__doc__)
    main()
    cv.destroyAllWindows()
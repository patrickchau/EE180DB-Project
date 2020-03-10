#!/usr/bin/env python
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

# local modules
from video import create_capture
from common import clock, draw_str
import color_detection as cd

#TODO: dynamically size the rectangle based on face to pass to unity,
#      intelligently decide which face is which
#      keep records of old faces for up to 5 cycles before removing

# UDP Connection Information
UDP_IP = '127.0.0.1'
UDP_PORT = 5065
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
abs_path = os.path.dirname(__file__)                       # local path
rel_path = 'img.png'
_GREEN = (0, 255, 0)
_RED = (0, 0, 255)
_BLUE = (255, 0, 0)
_YELLOW = (0, 255, 255)
color = (_BLUE, _YELLOW, _RED, _GREEN)
_MAX_CYCLE_COUNT_ = 10
__DEBUG__ = 0
_FIRST_PLACE = 1

# detects all the faces and assigns bounding boxes to them
def detect(img, cascade): 
    rects = cascade.detectMultiScale(img, scaleFactor=1.1, minNeighbors=4, minSize=(30, 30),
                                     flags=cv.CASCADE_SCALE_IMAGE)
    if len(rects) == 0:
        return []
    rects[:,2:] += rects[:,:2]
    return rects

# given a set of coords, draw the rectangles onto the image
def draw_rects(img, rects):
    # rects will go in [blue, yellow, green, red] just as in color_detection
    count = 0
    for x1, y1, x2, y2 in rects:
        cv.rectangle(img, (x1, y1), (x2, y2), color[count], 2)
        count += 1

# finds the rectangles for the faces
def find_rects(img, cascade):
    gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
    gray = cv.equalizeHist(gray)
    rects = detect(gray, cascade) # the large bounding boxes around faces
    return rects

def get_centroids(rects):
    cents = []
    for x1, y1, x2, y2 in rects:
        x_c = int((x2-x1)/2) + x1
        y_c = int((y2-y1)/2) + y1
        cents.append((x_c, y_c))
    return cents

def update_rects(old, new, cycle_count):
    # create likelihood measurements to see which faces update to
    # get the centroids
    m_cycle_count = cycle_count
    #print("Length of old faces:" + str(len(old)))
    #print("Length of new faces:" + str(len(new)))
    if( len(new) < len(old) and cycle_count < _MAX_CYCLE_COUNT_):
        m_cycle_count +=1 
        m_new = old
    else:
        m_new = new
        m_cycle_count = 0
    m_old = old
    #print("Old faces:" + str(m_old))
    #print("New faces: " + str(m_new))
    old_cen = get_centroids(m_old)
    new_cen = get_centroids(m_new)
    rect_update = []
    distances = []
    for x1, y1 in old_cen:
        temp_dist = []
        for x2, y2 in new_cen:
            temp_dist.append( math.sqrt(math.pow((x2-x1),2) + math.pow((y2-y1),2)) )
        distances.append(temp_dist)
    # distances will be a list of lists
    # entries 0-3: distances from first face, entries 4-7 distances from second face...
    # print("All calculated distances: " + str(distances))
    i = 0
    ind = []
    amt = len(m_new)
    # if len(new) < len(old), then substitute the index not chosen with (0,0,0,0)
    # or simply carry over the old value for 5 cycles
    #print("Number of faces found:" + str(amt))
    # old_cen indices will be in the same order as old
    if amt == 0:
        rect_update = [(0,0,0,0)]
    else:
        for cur_dis_list in distances:
            min_dis = min(cur_dis_list)
            min_idx = cur_dis_list.index(min_dis)
            # min_idx would be the index of the minimum face in new faces
            if min_idx in ind:
                # tiebreaker
                retrieve = ind.index(min_idx)
                old_dis = distances[retrieve]
                min_old_dis = min(old_dis)
                #print("Retrieved index: " + str(retrieve))
                if min_old_dis < min_dis:
                    # index already there wins the tiebreaker, choose next unique smallest in current dis list
                    sorted_dis = cur_dis_list.copy()
                    sorted_dis.sort()
                    #print("Sorted distances: " + str(sorted_dis))
                    enum = 1
                    # ensure uniqueness
                    while(enum < len(sorted_dis)):
                        if cur_dis_list.index(sorted_dis[enum]) in ind: 
                            enum += 1
                        else:
                            min_idx = cur_dis_list.index(sorted_dis[enum])
                            enum = len(sorted_dis)
                else:
                    #print("Retrieved distances: " + str(old_dis))
                    sorted_ret = old_dis.copy()
                    sorted_ret.sort()
                    # update older index with second smallest, and append current min_idx
                    enum = 1
                    while(enum < len(sorted_ret)):
                        if old_dis.index(sorted_ret[enum]) in ind: 
                            enum += 1
                        else:
                            min_idx = old_dis.index(sorted_ret[enum])
                            enum = len(sorted_ret)
            ind.append(min_idx)    
        # now that all existing inds are updated, see if we can add more
        # check to see what indices don't exist and then add them
        if (len(ind) < amt):
            indices = list(range(0, amt))
            remainder_set = set(indices) - set(ind)
            for addition in remainder_set:
                if (len(ind) < 4):
                    ind.append(addition)
        # now remove any repeated indices
        working_ind = ind.copy()
        has_seen = []
        m = 0
        while m < len(working_ind):
            if working_ind[m] in has_seen:
                working_ind.pop( m )
                m -= 1
            else:
                has_seen.append(working_ind[m])
            m += 1
        ind = working_ind
         # now all values are updated and unique
        for val in ind:
            rect_update.append(m_new[val])

        # if update list is greater than 4, truncate to only 4
        if len(rect_update) > 4:
            rect_update = rect_update[0:4]
        #print("Chosen update points" + str(rect_update))
        #print("Old update points" + str(old))
    return (rect_update, m_cycle_count)

def sortFunc(e):
    return e[0]

def readServer(c): # take in server messages
  buffer = None
  c.settimeout(0.001)  # wait 1 second before throwing an exception
  try:
    mess, address = c.recvfrom(1024)
    buffer = mess.decode('utf-8')
  except socket.timeout: 
    print("buffer is empty")
  return buffer

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

    # values used in the calculation
    _FIRST_PLACE = 1
    cycle_count = 0

    #functions for cascade and nested face detection
    cascade_fn = args.get('--cascade', abs_path+"/haarcascades/haarcascade_frontalface_alt.xml")
    nested_fn  = args.get('--nested-cascade', abs_path+"/haarcascades/haarcascade_eye.xml")

    # files provided from OpenCV sample file for face detection
    cascade = cv.CascadeClassifier(cv.samples.findFile(cascade_fn))
    nested = cv.CascadeClassifier(cv.samples.findFile(nested_fn))

    # 'convenience function for capture creation'
    cam = create_capture(video_src, fallback='synth:bg={}:noise=0.05:size=1280x1024'.format(cv.samples.findFile(abs_path+'/lena.jpg')))
    
    img = cv.imread(abs_path+"/index2.jpg")
    #_ret, img = cam.read()

    # take an initial reading of the rects
    # from left to right from the view of the camera, we will have players 1 to 4 w/ 0,0 as the upper left
    # blue = player 1, yellow = player 2, green = player 3, red = player 4
    rects = [(0,0,0,0)]
    new_rects = find_rects(img, cascade)
    rects, cycle_count = update_rects(rects, new_rects, cycle_count)
    rects=sorted(rects,key=sortFunc)
    # test image
    
    # continuously check the camera and update the bounding boxes 
    while True:
        # first retrieve message from game to see which face to display
        place = readServer(sock)
        if place != None and place != "nothing":
            _FIRST_PLACE = int(place)
            print("First place is now " + str(_FIRST_PLACE))
        # read from the camera and turn into grayscale
        #_ret, img = cam.read()
        
        # find the bounding boxes for faces
        new_rects = find_rects(img, cascade)
        rects, cycle_count = update_rects(rects, new_rects, cycle_count)
        
        # clock for measuring time in between frames
        t = clock() 

        # draws the rectangles that make up the bounding box
        # vis is the copy of the raw image that we will use for processing, so as to not mess up raw image
        vis = img.copy()
        draw_rects(vis, rects)

        # find_color_point will return an image with all other colors except desired color masked out
        # will also have the image of the circle on it
        # see color_detection.py
        # color is 1 = blue, 2 = yellow, 3 = green, 4 = red
        """
        out = cd.find_color_point(3, vis)
        centers = get_centroids(rects)
        for center in get_centroids(rects):
            #print()
            #print(center)
            cv.circle(vis, center, 5, (0, 0, 255), -1)
        # adds the timestamp in top left
        dt = clock() - t
        draw_str(vis, (20, 20), 'time: %.1f ms' % (dt*1000))
        """
        # draw the image after all has been put onto it
        cv.imshow('facedetect', vis)

        # goal here is to get the circle and the raw image 
        # so we need to pass the center and the image on the raw image
        # currently set to only save the image of the person on the left hand side of screen

        
        if (len(rects) > 0):    # if rects is not empty, update on unity end
            """
            origin = out # the point from which distances are calc'd
            #print(origin)
            (x_marg, y_marg) = (0,0)
            (x1,x2,y1,y2) = (0,0,0,0)
            smallest = 25000
            
            for update in rects:
                co = "" # x1 y1 x2 y2
                # for the given origin, find the smallest norm from (x1,y1) of the rect
                if math.sqrt( math.pow((origin[0] - update[0]),2) + math.pow((origin[1] - update[1]),2)  )  < smallest:
                    # if a new smallest found, then update the coords
                    count = 0
                    for coord in update:
                        co += str(coord) + " "
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
                sock.sendto( (co).encode(), (UDP_IP, UDP_PORT) 
            """
            update = rects[_FIRST_PLACE - 1]
            (x_marg, y_marg) = (0,0)
            (x1,x2,y1,y2) = (0,0,0,0)

            count = 0
            for coord in update:
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

            # save the cropped image for unity to pull
            crop_img = img[y1:y1+y_marg, x1:x1+x_marg].copy()
            if (crop_img.size != 0): # only write if the image actually exists
                cv.imwrite('fullimage.jpg', img)        # saves raw image 
                cv.imwrite('savedImage.jpg', crop_img)  # saves face image for unity
                sock.sendto( 'updated'.encode(), (UDP_IP, UDP_PORT) )   # sends the coords for face
            
            
        # exit out of program if pressing escape
        if cv.waitKey(5) == 27:
            break
    #print('Done')

if __name__ == '__main__':
    #print(__doc__)
    main()
    cv.destroyAllWindows()
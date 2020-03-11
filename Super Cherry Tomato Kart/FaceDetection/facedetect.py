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
# import color_detection as cd

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
        if count < 4:
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

def calc_distances( old_cen, new_cen ):
    distances = []
    for x1, y1 in old_cen:
        temp_dist = []
        for x2, y2 in new_cen:
            temp_dist.append( math.sqrt(math.pow((x2-x1),2) + math.pow((y2-y1),2)) )
        distances.append(temp_dist)
    return distances

def update_rects(old, new, cycle_count):
    # create likelihood measurements to see which faces update to
    # get the centroids
    rect_update = []

    # this block checks persistence and only updates to the new reading if enough cycles have passed
    m_cycle_count = cycle_count
    if( len(new) < len(old) and cycle_count < _MAX_CYCLE_COUNT_):
        m_cycle_count +=1 
        m_new = old
    else:
        m_new = new
        m_cycle_count = 0
    m_old = old

    # calculate distances of all centroids
    old_cen = get_centroids(m_old)
    new_cen = get_centroids(m_new)
    distances = calc_distances(old_cen, new_cen)

    # distances will be a list of lists
    # entries 0-3: distances from first face, entries 4-7 distances from second face...
    ind = []           # list for all indices of faces
    amt = len(m_new)   

    # if the new update detects no faces, just make one at 0,0
    if amt == 0:
        rect_update = [(0,0,0,0)]
    else:
        # otherwise for each distance list in distances
        for cur_dis_list in distances:
            # find minimum and the index of the minimum
            min_dis = min(cur_dis_list)
            min_idx = cur_dis_list.index(min_dis)   # min(distances [i, j]) = for face i, face j has the smallest distance from it 
            # so min_idx = minimum face for the current face that we're looking at
            # however need to check if it's already chosen since we want all faces to be unique
            if min_idx in ind:
                # tiebreaker
                # so find the index of the previously allocated
                # this index means that for face [retrieve], face j is also the smallest distance away
                retrieve = ind.index(min_idx)
                old_dis = distances[retrieve] # pull the list of distances for face[retrieve] and find the minimum distance
                min_old_dis = min(old_dis)
                # min_old_dis will be the distance between face[retrieve] and face j
                # if face i has the smaller distance from face j compared to face[retrieve], then face i win thes the tiebreaker
                if min_old_dis < min_dis:
                    # index already there wins the tiebreaker, choose next unique smallest in current dis list
                    # so we then look through the distances and look at the next smallest and so on.
                    sorted_dis = cur_dis_list.copy()
                    sorted_dis.sort()
                    enum = 1
                    # ensure uniqueness
                    while(enum < len(sorted_dis)):
                        if cur_dis_list.index(sorted_dis[enum]) in ind: 
                            enum += 1
                        else:
                            min_idx = cur_dis_list.index(sorted_dis[enum])
                            enum = len(sorted_dis)
                else:
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

def main():

    # values used in the calculation
    _FIRST_PLACE = 1
    cycle_count = 0

    # first see if there is access to a webcam to use as source
    args, video_src = getopt.getopt(sys.argv[1:], '', ['cascade=', 'nested-cascade='])
    try:
        video_src = video_src[0]
    except:
        video_src = 0
    args = dict(args)

    # pull in the haar cascade classifiers
    # see: https://docs.opencv.org/3.4/db/d28/tutorial_cascade_classifier.html

    #functions for cascade and nested face detection
    cascade_fn = args.get('--cascade', abs_path+"/haarcascades/haarcascade_frontalface_alt.xml")
    nested_fn  = args.get('--nested-cascade', abs_path+"/haarcascades/haarcascade_eye.xml")

    # files provided from OpenCV sample file for face detection
    cascade = cv.CascadeClassifier(cv.samples.findFile(cascade_fn))
    nested = cv.CascadeClassifier(cv.samples.findFile(nested_fn))

    # 'convenience function for capture creation'
    cam = create_capture(video_src, fallback='synth:bg={}:noise=0.05:size=1280x1024'.format(cv.samples.findFile(abs_path+'/lena.jpg')))
    
    #img = cv.imread(abs_path+"/5.jpg")
    _ret, img = cam.read()

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
        _ret, img = cam.read()
        
        # find the bounding boxes for faces
        new_rects = find_rects(img, cascade)
        rects, cycle_count = update_rects(rects, new_rects, cycle_count)
        
        # clock for measuring time in between frames
        t = clock() 

        # draws the rectangles that make up the bounding box
        # vis is the copy of the raw image that we will use for processing, so as to not mess up raw image
        vis = img.copy()
        draw_rects(vis, rects)

        # draw the image after all has been put onto it
        cv.imshow('facedetect', vis)

        if (len(rects) > 0):    # if rects is not empty, update on unity end
            if len(rects) < _FIRST_PLACE:
                update = rects[0]
            else:
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
                cv.imwrite('savedImage.jpg', crop_img)  # saves face image for unity
                sock.sendto( 'updated'.encode(), (UDP_IP, UDP_PORT) )   # sends the coords for face

        if cv.waitKey(5) == 27:
            break

if __name__ == '__main__':
    #print(__doc__)
    main()
    cv.destroyAllWindows()
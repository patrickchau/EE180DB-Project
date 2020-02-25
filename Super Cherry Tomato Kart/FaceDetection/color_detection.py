import numpy as np
import argparse
import cv2

# values given in HSV space
boundaries = [
    ([86,0,0],[120,255,255]),       # blue
    ([20,0,0],[35,255,255]),        # yellow
    ([40,0,0],[80,255,255]),        # green
    ([0,50,20],[15,255,255])        # red 
]

# need to return the center and draw circles on HSV image

# code from pyimagesearch
def grab_contours(cnts):
    if len(cnts) == 2:
        cnts = cnts[0]
    elif len(cnts) == 3:
        cnts = cnts[1]
    else:
        raise Exception(("Contours tuple must have length 2 or 3, "
            "otherwise OpenCV changed their cv2.findContours return "
            "signature yet again. Refer to OpenCV's documentation "
            "in that case"))
    # return the actual contours array
    return cnts

# given a mask and the image, find the largest contour and the center of it
def find_color_point( color, image ):
    hsv = cv2.cvtColor(image.copy(), cv2.COLOR_BGR2HSV) # convert to HSV values
    mask = get_color_mask(color, hsv.copy())            # get the bitmap of colors which match the boundary chosen
    output = cv2.bitwise_and(hsv, hsv, mask=mask)       # apply mask to image

    # find contours in the image
    cnts = cv2.findContours(mask.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    # function to ensure compatibility with any version of opencv
    cnts = grab_contours(cnts)
    center = None

    # given the contours, find a minimum bounding circle
    if len(cnts) > 0:
        # if there are found contours, find the largest one
        c = max(cnts, key=cv2.contourArea)
        ((x,y), radius) = cv2.minEnclosingCircle(c)
        M = cv2.moments(c)
        center = (int(M["m10"] / M["m00"]), int(M["m01"] / M["m00"]))

	# only proceed if the radius meets a minimum size
    if radius > 10:
		# draws the circle onto the original image
        cv2.circle(image, (int(x), int(y)), int(radius),
            (0, 255, 255), 2)
        cv2.circle(image, center, 5, (0, 0, 255), -1)
    
    # convert the masked image back to BGR for display
    converted = cv2.cvtColor(output,cv2.COLOR_HSV2BGR)
    return converted

def get_color_mask(color, image):
    # assuming that image is given in HSV
    mask = None
    if color >= 0 and color <= 3:
        low = np.array(boundaries[color][0], dtype = "uint8")   # pull the correct color boundaries
        up = np.array(boundaries[color][1], dtype = "uint8")
        mask = cv2.inRange(image.copy(), low, up)             # mask the image according to boundary
        mask = cv2.erode(mask, None, iterations = 2) 
        mask = cv2.dilate(mask, None, iterations = 2) # removes small bumps outside of main
    return mask # returns the mask and the masked image

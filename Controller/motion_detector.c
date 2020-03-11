/*************************************************************************/
/*                          hardware_token.c                             */
/*************************************************************************/
/*                       This file is part of:                           */
/*                          Motion Detector                              */
/*           https://github.com/patrickchau/ECE180D_Project              */
/*************************************************************************/
/*                 Copyright  2-19-2020 Joseph Miller.                   */
/*                                                                       */
/* Permission is hereby granted, free of charge, to any person obtaining */
/* a copy of this software and associated documentation files (the       */
/* "Software"), to deal in the Software without restriction, including   */
/* without limitation the rights to use, copy, modify, merge, publish,   */
/* distribute, sublicense, and/or sell copies of the Software, and to    */
/* permit persons to whom the Software is furnished to do so, subject to */
/* the following conditions:                                             */
/*                                                                       */
/* The above copyright notice and this permission notice shall be        */
/* included in all copies or substantial portions of the Software.       */
/*                                                                       */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,       */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF    */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.*/
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY  */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,  */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE     */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                */
/*************************************************************************/

#include "motion_detector.h"
#include <sys/time.h>
#include <stdint.h>
#include <unistd.h>
#include <math.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <fcntl.h>
#include <time.h>
#include "IMU/IMU.c"
#include "signal_handler.h"
#include "globals.h"

int mymillis()
{
	struct timeval tv;
	gettimeofday(&tv, NULL);
	return (tv.tv_sec) * 1000 + (tv.tv_usec)/1000;
}

int timeval_subtract(struct timeval *result, struct timeval *t2, struct timeval *t1)
{
    long int diff = (t2->tv_usec + 1000000 * t2->tv_sec) - (t1->tv_usec + 1000000 * t1->tv_sec);
    result->tv_sec = diff / 1000000;
    result->tv_usec = diff % 1000000;
    return (diff<0);
}

void* detect_motion(void* arg)
{

    // Remove compiler warning
    (void)arg;
	
	// Mask sigpipe on this thread.
    mask_sigpipe();

	float rate_gyr_y = 0.0;   // [deg/s]
	float rate_gyr_x = 0.0;   // [deg/s]
	float rate_gyr_z = 0.0;   // [deg/s]

	int  accRaw[3];
	int  gyrRaw[3];

	//Not using magRaw
	int  magRaw[3];
	(void)magRaw;

	float gyroXangle = 0.0;
	float gyroYangle = 0.0;
	float gyroZangle = 0.0;
	float AccYangle = 0.0;
	float AccXangle = 0.0;
	float CFangleX = 0.0;
	float CFangleY = 0.0;

	int startInt  = mymillis();
	struct  timeval tvBegin;

	while(!detectIMU());
	enableIMU();

	gettimeofday(&tvBegin, NULL);

	while(!program_end)
	{
		startInt = mymillis();

		//read ACC and GYR data
		readACC(accRaw);
		readGYR(gyrRaw);

		//Convert Gyro raw to degrees per second
		rate_gyr_x = (float) gyrRaw[0] * G_GAIN;
		rate_gyr_y = (float) gyrRaw[1]  * G_GAIN;
		rate_gyr_z = (float) gyrRaw[2]  * G_GAIN;

		//Calculate the angles from the gyro
		gyroXangle+=rate_gyr_x*DT;
		gyroYangle+=rate_gyr_y*DT;
		gyroZangle+=rate_gyr_z*DT;

		//Convert Accelerometer values to degrees
		AccXangle = (float) (atan2(accRaw[1],accRaw[2])+M_PI)*RAD_TO_DEG;
		AccYangle = (float) (atan2(accRaw[2],accRaw[0])+M_PI)*RAD_TO_DEG;

		//If IMU is up the correct way, use these lines
		AccXangle -= (float)180.0;
		if (AccYangle > 90)
				AccYangle -= (float)270;
		else
			AccYangle += (float)90;

		//Complementary filter used to combine the accelerometer and gyro values.
		CFangleX=AA*(CFangleX+rate_gyr_x*DT) +(1 - AA) * AccXangle;
		CFangleY=AA*(CFangleY+rate_gyr_y*DT) +(1 - AA) * AccYangle;

		// Determine if we should turn right or left
		pthread_mutex_lock(&lock);

		if(CFangleX > 30.0f) {
			right_pushed = 1;
			left_pushed = 0;
		}
		else if(CFangleX < -30.0f) {
			right_pushed = 0;
			left_pushed = 1;
		} else{
			right_pushed = 0;
			left_pushed = 0;
		}

		pthread_mutex_unlock(&lock);

		//printf ("   CFangleX  %7.3f \t CFangleY %7.3f\n",CFangleX,CFangleY);

		//Each loop should be at least 20ms.
		while(mymillis() - startInt < (DT*1000)){
				usleep(100);
		}

	//	printf("Loop Time %d\t", mymillis()- startInt);
    }

	fprintf(stdout, "Motion thread exiting...\n");
    return NULL;
}


﻿using System;
using System.Text;
using System.Threading;
using VzenseNebula_enums;
using VzenseNebula_types;
using VzenseNebula_api;

namespace DeviceHWTriggerMode
{
    using VzDeviceHandle = System.IntPtr;
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("---DeviceHWTriggerMode---");

            //about dev
            VzenseNebulaAPI VNAPI = new VzenseNebulaAPI();
            UInt32 deviceCount = 0;
            
            VzDeviceHandle deviceHandle = new IntPtr();
            VzReturnStatus status = VzReturnStatus.VzRetOthers;

            //about frame

            VzFrameReady FrameReady = new VzFrameReady();
            VzFrame depthFrame = new VzFrame();

            //SDK Initialize
            status = VNAPI.VN_Initialize();
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VzInitialize failed status:" + status);
                Console.ReadKey(true);
                return;
            }

        //1.Search and notice the count of devices.
        //2.get infomation of the devices. 
        //3.open devices accroding to the info.
        GET:
            status = VNAPI.VN_GetDeviceCount(ref deviceCount);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VzGetDeviceCount failed! make sure the DCAM is connected");
                Thread.Sleep(1000);
                goto GET;
            }
            Console.WriteLine("Get device count: " + deviceCount);
            if (0 == deviceCount)
            {
                Thread.Sleep(1000);
                goto GET;
            }

            VzDeviceInfo[] pDeviceListInfo = new VzDeviceInfo[deviceCount];
            status = VNAPI.VN_GetDeviceInfoList(deviceCount, pDeviceListInfo);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("GetDeviceListInfo failed status:" + status);
                return;
            }
            else
            {
                if (VzConnectStatus.VzConnected != pDeviceListInfo[0].status)
                {
                    Console.WriteLine("connect statu" + pDeviceListInfo[0].status);
                    Console.WriteLine("Call VN_OpenDevice with connect status :" + VzConnectStatus.VzConnected);
                    return;
                }
            }

            Console.WriteLine("uri:" + pDeviceListInfo[0].uri);
            Console.WriteLine("alias:" +pDeviceListInfo[0].alias);
            Console.WriteLine("ip:" + pDeviceListInfo[0].ip);
            Console.WriteLine("connectStatus:" + pDeviceListInfo[0].status);

            status = VNAPI.VN_OpenDeviceByUri(pDeviceListInfo[0].uri, ref deviceHandle);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("OpenDevice failed status:" + status);
                return;
            }

            //Starts capturing the image stream
            status = VNAPI.VN_StartStream(deviceHandle);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VN_StartStream failed status:" + status);
                return;
            }

            //Wait for the device to upload image data
            Thread.Sleep(1000);

            //set slave true
            status = VNAPI.VN_SetWorkMode(deviceHandle, VzWorkMode.VzHardwareTriggerMode);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VN_SetWorkMode failed status:" + status);
                return;
            }

            //Clearing cached images
            for (int i = 0; i < 5; i++)
            {
                status = VNAPI.VN_GetFrameReady(deviceHandle, 200, ref FrameReady);
            }

            Console.WriteLine("Please trigger the hardware signal to start the hardware trigger test");

            //1.hardware trigger.
            //2.ReadNextFrame.
            //3.GetFrame acoording to Ready flag and Frametype.
            for (int i = 0; i < 20; i++)
            {
                //The minimum time interval to trigger a signal is 1000/FPS milliseconds
                //Wait for an external trigger signal. If the external signal is triggered once, the device sends a frame
                //If no image is ready within 1200ms, the function will return VzRetGetFrameReadyTimeOut
                status = VNAPI.VN_GetFrameReady(deviceHandle, 1200, ref FrameReady);
                if (status != VzReturnStatus.VzRetOK)
                {
                    Console.WriteLine("VN_GetFrameReady failed status:" + status);
                    continue;
                }

                //depthFrame for example.
                if (1 == FrameReady.depth)
                {
                    status = VNAPI.VN_GetFrame(deviceHandle, VzFrameType.VzDepthFrame, ref depthFrame);
                    if (depthFrame.pFrameData != IntPtr.Zero)
                    {
                        Console.WriteLine("get Frame successful,status:" + status + "  "
                        + "frameTpye:" + depthFrame.frameType + "  "
                        + "frameIndex:" + depthFrame.frameIndex);
                    }
                }

            }

            //set slave false
            status = VNAPI.VN_SetWorkMode(deviceHandle, VzWorkMode.VzActiveMode);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VN_SetWorkMode failed status:" + status);
                return;
            }
            status = VNAPI.VN_StopStream(deviceHandle);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VN_StopStream failed status:" + status);
                return;
            }

            //1.close device
            //2.SDK shutdown
            status = VNAPI.VN_CloseDevice(ref deviceHandle);
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VN_CloseDevice failed status:" + status);
                return;
            }
            status = VNAPI.VN_Shutdown();
            if (status != VzReturnStatus.VzRetOK)
            {
                Console.WriteLine("VN_Shutdown failed status:" + status);
                return;
            }
            Console.WriteLine("---end---");

            return ;
        }
    }
}
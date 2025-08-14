# Online 3D Reconstruction Pipeline for MC of rovers

A 3D Reconstruction pipeline that generates colored meshes online of the rover's surroundings and renders them in Unity. 

![Intro-image-compressed](https://github.com/user-attachments/assets/082b9080-b679-4d4e-b4d4-0464d60abec7)

# Description

The pipeline is a Unity-based Virtual Reality (VR) application that generates colored 3D meshes using Truncated Signed Distance Functions (TSDFs) and the Marching Cubes surface reconstruction algorithm. It is modular enough to be easily integrated with any mobile robotic system, as long as the robot, and it's sensors, are being used with ROS2. 

# Overview

![3D-Pipeline-Architecture-final](https://github.com/user-attachments/assets/d900895e-7b0d-457b-8be2-abffc40b6b0b)

The 3D reconstruction pipeline is separated into two main systes:

1. ROS2 system (ROS2 Humble)
2. VR system (Unity)

Communication and data transfer between the two systems is possible using Unity's [ROS-TCP Connector][https://github.com/Unity-Technologies/ROS-TCP-Connector]. 

The 3D reconstruction pipeline expects a ROS2 message of PointCloud2 type, which is then read by a dedicated ROS2 subscriber script in Unity. The point cloud is then sent to the TSDF Constructor to produce a TSDF, and then to the Mesh Generator, which uses the Marching Cubes algorithm to reconstruct the mesh from the TSDF data. This procedure is performed online (near real-time), as long as there is Point Cloud data sent over from the rover. 

The 3D mesh is rendered inside a VR scene, where the user can teleport around and drive the rover using the VR controllers


# Video

A video of the online 3D reconstruction pipeline being used can be found [here][https://www.youtube.com/watch?v=WibrHY4XUO8&t=45s]

# Supported Software Versions

The pipeline has been used with the following software versions:

1. ROS2 Humble (Jazzy should be possible but not tested)
2. Unity3D Game Engine (any version above 2020.x should work)

# Supported Hardware 

Regarding hardware, we have used the HTC Vice cosmos elite HMD for VR interaction. However, any HMD that is supported by the OpenVR Unity plugin can be used. 

# Installation

comming soon...

# Usage

coming soon...

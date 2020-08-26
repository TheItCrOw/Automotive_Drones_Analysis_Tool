![Start](https://user-images.githubusercontent.com/49918134/91304500-dce97c80-e7a9-11ea-97fb-88cfe2390f91.jpg)

Automotive Drone Analysis Tool (short: ADAT) is a Windows only, desktop based application, that was developed as a submition for the Code Competition held by IAV GmbH. The application reached first place and therefore won the competition alongside its price. ([See here](https://www.it-talents.de/blog/partnerunternehmen/kevin-holt-den-ersten-platz-bei-der-code-competition-der-iav))

## Technologies

* C#
* .NET Stack / .NET Core 3.0
* WPF => MVVM
* Visual Studio 2019
* Git
* Material Design
* YOLO (You-Only-Look-Once) Real-Time Object Detection
* Open Source Nerual Network Darknet

## About

![Home](https://user-images.githubusercontent.com/49918134/91314517-efb67e00-e7b6-11ea-950b-6606f9aa501f.png)

ADAT is a tool that loads in drone images of certain traffic scenarios (e.g. parking cars) and detects all objects in them. The user may then adjust the drawn bounding boxes of these objects and adds a number of additional information (such as length of certain lines) to the images. After having prepared the image, the user can then analyse the picture in the dynamic report view, where he can see distances between objects, angles to each other and lengths of each drawn line. Please refere to "Usage" for a quick example.

## Usage

![Select Medium](https://user-images.githubusercontent.com/49918134/91315866-7750bc80-e7b8-11ea-942d-39e17ca732ea.png)

To start the analysis, the user must first choose between: Loading in images or single video files.

### Analyse images



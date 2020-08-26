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

To start the analysis, the user must first choose between: Loading in images or video files.

![Select Medium](https://user-images.githubusercontent.com/49918134/91315866-7750bc80-e7b8-11ea-942d-39e17ca732ea.png)

### Analyse images

After having chosen which images to analyse, YOLO tries to detect all objects in them which may look like this: 

![Nach YOLO Analyse](https://user-images.githubusercontent.com/49918134/91316613-4d4bca00-e7b9-11ea-88a7-d1d1435546fc.jpg)

The user must then add more information to the already found objects to guarantee a meaningful analysis. A step by step guide can be found on the right side of the application named "Checklist". A fully prepared image may then look like this:

![prepariertes image](https://user-images.githubusercontent.com/49918134/91317485-65701900-e7ba-11ea-9c62-a782077ac4df.jpg)

From there the user may enter the dynamic report view, which should look like this after correctly preparing the image:

![Dynamic Report View (2)](https://user-images.githubusercontent.com/49918134/91317139-f85c8380-e7b9-11ea-9932-886dc410a53f.jpg)

Every YOLO object is selectable and can be enabled or disabled, causing all reference lines and values of this object to also be visible or non visible. That way, the user can choose which objects he would like to analyse specifically without the screen being convoluted by to much information. This analysis can also be exported to pdf as seen here:

![PDF Export 3](https://user-images.githubusercontent.com/49918134/91318650-be8c7c80-e7bb-11ea-9718-9edde572e539.png)

### Analyse videos

ADAT can also analyse videos of e.g. parking cars using the same formular. Via a small built in video player, the user may then watch the analysed video. You can find an example below:

<iframe src="https://giphy.com/embed/QzBAQUfqRPacTG1zV2" width="480" height="252" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/QzBAQUfqRPacTG1zV2">via GIPHY</a></p> 

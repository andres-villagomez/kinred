# Audio-Signal-Recognition

This is an Audio Signal Recognition like Cortana, Siri, Alexa or Ok Goole

## How does the voice recognition is made of?

This application was built using 5 components.

* Microsoft Speech Lib (Version year 2013) 
* Kinect Firmware Lib (Version year 2011) 
* Microsoft English Dictionary (Version year 2013) 
* Kinect Hardware (Version 1) 
* Visual Studio (Version 2008) 

Note: This project doesn't use any version of Microsoft SDK

## How does the voice recognition works?

Kinect Firmware Lib allows to connect Kinect with Visual Studio, Kinect Firmware Lib was an unofficial and unsupported library released in 2011. (No download link resource available)

Once Kinect is working with Visual Studio correctly, Microsoft Speech Lib takes the voice captured by Kinect and turn the speech to text. 

Then the text is recognized by comparing it with the Microsoft English Dictionary. 

Depending on the result an action will be performed.

### Microsoft Speech Lib

Microsoft Speech Lib contains the functions to perform speech-to-text activity called “speech-recognition/SR”. It enables real-time transcription of audio streams into text. This service is powered by the same recognition technology that Microsoft uses for Cortana and Office products, and works seamlessly with the translation and text-to-speech.

Microsoft English Dictionary

A dictionary is required for the speech to text recognition, since it matches the words and select the results.

## How does the kinect speech recognition is initialized?

## How does voice commands perform actions?

The way is very rudimentary. But Guess What D: ? Today speech recognition platforms like Siri works like this! And I programmed this 8 year ago... !

A Case Loop with several Microsoft commands was programmed, it selects the result given by the dictionary in the match process and call the selected process. 

Once the command is submitted, it is compared in a case loop, so that the word registered entails the program to perform specific predefined actions.

## How does the speech is interpreted?

A grammar function is used to set the word parameters, because the dictionary is big and the application is not deployed in a Cloud environment, this method will index the words that we are trying to interpret. A word before the command we are trying to identify must be written in order to prevent the accidental command activation while speaking with other people. (This is a common mistake made even by first world companies, for example like Bancomer)

## Future work for this project

This work is obsolete, no applications must derive from it.

* It has a strong dependency with Kinect which is why is not profitable.
* Also it does not work with artificial intelligence.
* It does not utilize restful services so it is not useful for any company.
* Finally the source code is made in Visual Basic, then there is no evolution guarantee.
* It was made of deprecated libraries and outdated Visual Studio version. 
* I made it in 2011, so its 7 years aged Today there are much faster, more efficient, more scalable and easier solutions to implement today, that perform the same job but in a better way.
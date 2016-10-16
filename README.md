InsightPreprocessor
===================

Description
-----------

This application was designed as part of a PhD research project to visualise the output of an Autopsy digital forensics investigation in a 2D timeline format.
This preprocessor reads a SQLite database generated by the Autopsy 3 digital forensics applcation, and then generates an index file which is used by the main Insight application.
The main Insight application is hosted in a separate repo.

It is no longer actively developed, and the code provided is presented 'as-is'. It contains known bugs which are not intended to be fixed.

The thesis which relates to this project can be found here:
https://repository.abertay.ac.uk/jspui/handle/10373/2413


Requirements
------------

This project is designed to run on Windows and targets version 4.5 of the .NET framework.


Licence
-------

This project is distributed under the terms of the GNU GPL v3 licence.


Instructions
------------

To use this application, you must first run an ingest on a disk image using the Autopsy 3 application, which will produce an 'autopsy.db' file.
You should move this file into a 'datasets' directory in a suitable place. To keep things simple, it is recommended that you create this directory in the same location as your InsightPreprocessor.exe file.
Run the application and you will be asked for the location of your project directory. This is the parent path of your 'datasets' directory. If you have created the directory in the same place as the exe file, you can simply enter a period '.' to use the current working directory.
The application should read relevant information from the SQLite database and then write it to an xml file called 'insight.xml'.
This file can then be moved to the same directory as your main Insight.exe file.


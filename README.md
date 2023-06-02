# ReplayHistoryData
ReplayHistoryData is an application that replays old data from one PI server to another. It reads data from multiple source tags and writes it to multiple destination tags with new timestamps based on the current time and the time differences between the original timestamps.
## Configuration
The application can be configured using the `app.config` file. The following settings can be specified in the `appSettings` section of the file:

* `eventGeneratorConfigFilePath`: The path to the event generator configuration file. This file should contain a JSON array of objects, where each object represents a job.
* `sdkReadIntervalInHours`: The interval in hours at which data is read from the source tags. If the time difference between the start time and end time for a job is greater than this value, then data will be read from the input tag in intervals of time defined by this value.

The application also uses log4net for logging. The log4net configuration can be specified in the `log4net` section of the `app.config` file. The application creates a `Logs` folder in the same folder as the `.exe` file and stores log files there. The log files are rotated every day with numbers 1, 2, 3, etc. The log file will be rotated if it reaches a size of 10MB or one day has passed.

## Input
The application takes a JSON file as input. The path to this file should be specified in the `eventGeneratorConfigFilePath` setting in the `app.config` file. The JSON file contains an array of objects, where each object represents a job. Each job has the following properties:

`Job_ID`: The ID of the job.
`Start_Time`: The start time of the job.
`End_Time`: The end time of the job.
`Input_Tag`: The input tag or tag mask.
`Input_PIServer`: The name of the input PI server.
`Output_Tag`: The output tag or tag mask.
`Output_PIServer`: The name of the output PI server.
Here is an example of a JSON file that can be used as input for the application:
```
[
  {
    "Job_ID": "1",
    "Start_Time": "01-Mar-2023",
    "End_Time": "02-Mar-2023",
    "Input_Tag": "testtag.*",
    "Input_PIServer": "sourceserver1",
    "Output_Tag": "Demo.testtag.*",
    "Output_PIServer": "destinationserver1"
  },
  {
    "Job_ID": "1",
    "Start_Time": "3/20/2023 13:38",
    "End_Time": "3/23/2023 8:30",
    "Input_Tag": "testtag",
    "Input_PIServer": "sourceserver2",
    "Output_Tag": "demo.testtag",
    "Output_PIServer": "destinationserver2"
  }
]
```
The application decides whether a tag is a tag name or a tag mask by checking whether it contains * or ?.

## Classes
The application consists of several classes:

`TagConfig`: This class is used to store information about the input and output tags and PI servers.
`ConfigValidator`: This class is used to validate the configuration of the application.
`Utility`: This class has various utility methods for checking if tags have the same data type, finding a PI server, checking if a PI server name is valid, checking if a tag exists, connecting to a PI server, and getting a PI server.
`LoggerUtility`: This class has methods for adding error and warning messages to lists and logging them using an ILog object.
`ErrorChecker`: This class has methods for checking if a tag config is valid and if the tag count is valid.
`DataReplayer`: This class is used to replay data from multiple source tags to multiple destination tags with new timestamps based on the current time and the time differences between the original timestamps.

## Running as a Service
You can use another application to run ReplayOldData as a service. The code for this application consists of two classes: `Service1` and `Program`. The `Service1` class inherits from the `ServiceBase` class and overrides the `OnStart()` and `OnStop()` methods. The `OnStart()` method starts a new process that runs the ReplayOldData `.exe` file and creates a timer to periodically check if the process is still running. The `OnStop()` method stops the timer and kills the process that runs the ReplayOldData `.exe` file. The `Program` class contains the `Main()` method, which creates an instance of the Service1 class and runs it as a service.

The application can be configured using the `app.config` file. The following setting can be specified in the `appSettings` section of the file:

* `exeFilePath`: The path to the ReplayOldData `.exe` file.

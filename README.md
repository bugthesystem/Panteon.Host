# Panteon.Host

![](https://github.com/PanteonProject/panteon-dashboard/blob/master/misc/path4141.png)  

Setup
=====================================================================

Install Panteon.Host as windows service executing following command
```sh
Panteon.Host.exe install
```

###App Settings

**PANTEON_JOBS_FOLDER**  
Jobs folder path  
**PANTEON_REST_API_URL**  
Jobs REST API start url  

Sample appSettings node;  
```xml
 <appSettings>
    <add key="PANTEON_JOBS_FOLDER" value="C:\@Panteon\Jobs"/>
    <add key="PANTEON_REST_API_URL" value="http://localhost:5002"/>
  </appSettings>
```

**TODO:**
- Nuget Package

# Panteon.Host

![](https://github.com/PanteonProject/panteon-dashboard/blob/master/misc/path4141.png)  

Setup
=====================================================================

###(Optional) Add an HTTP URL Namespace Reservation

This application listens to `http://localhost:8080/`. By default, listening at a particular HTTP address requires administrator privileges. When you run the host, therefore, you may get this error: `"HTTP could not register URL http://+:8080/"`   

There are two ways to avoid this error:

Use `Netsh.exe` to give your account permissions to reserve the URL.
To use `Netsh.exe`, open a command prompt with administrator privileges and enter the following command:  

```sh
netsh http add urlacl url=http://+:8080/ user=machine\username
``` 

For example;
```sh
netsh http add urlacl url=http://+:8080/ user=\Everyone
```
where `machine\username` is your user account.

When you are finished self-hosting, be sure to delete the reservation:
```sh
netsh http delete urlacl url=http://+:8080/
```



Install Panteon.Host as windows service executing following command
```sh
Panteon.Host.exe install
```

###App Settings

**PANTEON_JOBS_FOLDER**  
Jobs folder path  
**PANTEON_REST_API_URL**  
Jobs REST API start url 

#####(Optional) RealtimePanteonWorker Settings

**PS_APP_ID**  
Pusher App Id  
**PS_APP_KEY**  
Pusher App Key  
**PS_APP_SECRET**  
Pusher App Secret  

Sample appSettings node;  
```xml
 <appSettings>
    <add key="PANTEON_JOBS_FOLDER" value="C:\@Panteon\Jobs"/>
    <add key="PANTEON_REST_API_URL" value="http://+:8080/"/>
    <add key="PS_APP_ID" value="PusherAppId" />
    <add key="PS_APP_KEY" value="PusherAppKey" />
    <add key="PS_APP_SECRET" value="PusherAppSecret" />
  </appSettings>
```

**TODO:**
- Nuget Package

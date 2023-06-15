# Requirements for Track Your Day Application

## Event Registering

**Given**  
Track Your Day Application is working  
**When**  
Active Window changes  
**Then**  
Event is Registered with all of the following details:  
--Event Date  
--Active Window name

## Break Recording

**Given**   
Break Recording feature is Enabled  
**When**  
There is no activity for specified in Feature Settings amount of time  
OR  
User Session in Operating System is Blocked  
**Then**  
Break is Recorded

**Given**   
Break Recording feature is Enabled  
**When**  
Break Recording ends  
**Then**  
User can choose to Register Recorded Break with following details:  
--Break start time  
--Break end time  
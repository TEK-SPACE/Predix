/*how many in and out events per hour of the day*/
SELECT   TOP 1000 CONVERT(DATE, PE.CreatedDate) AS Date ,
                  DATEPART(HOUR, PE.CreatedDate) AS Hour ,
                  PE.EventType ,
                  COUNT(*) AS Events
FROM     dbo.ParkingEvents AS PE
GROUP BY PE.EventType ,
         CAST(PE.CreatedDate AS DATE) ,
         DATEPART(HOUR, PE.CreatedDate)
ORDER BY CONVERT(DATE, PE.CreatedDate) DESC ,
         DATEPART(HOUR, PE.CreatedDate) DESC;

/*are we getting ins and out for all transactions(by location Uid and Object id)*/
SELECT   TOP 1000 CONVERT(DATE, PE.CreatedDate) AS Date ,
                  PEP.LocationUid ,
                  PEP.ObjectUid ,
                  PE.EventType
FROM     dbo.ParkingEventProperties AS PEP
         JOIN dbo.ParkingEvents AS PE ON PE.PropertyId = PEP.Id
WHERE    PEP.LocationUid IS NOT NULL
GROUP BY CONVERT(DATE, PE.CreatedDate) ,
         PEP.LocationUid ,
         PEP.ObjectUid ,
         PE.EventType
ORDER BY CONVERT(DATE, PE.CreatedDate) DESC ,
         PEP.LocationUid ,
         PEP.ObjectUid ,
         PE.EventType;

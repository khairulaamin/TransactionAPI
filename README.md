docker build -t app .
docker run -p 8080:8080 app

use swagger 
http://localhost:8080/swagger/index.html


sample request
{
  "partnerkey": "FAKEGOOGLE",
  "partnerrefno": "FG-00001",
  "partnerpassword": "RkFLRVBBU1NXT1JEMTIzNA==",
  "totalamount": 120500,
  "items": [
    {
      "partneritemref": "i-00001",
      "name": "Pen",
      "qty": 1205,
      "unitprice": 100
    }
  
  ],
  "timestamp": "2025-03-09T13:15:30.0000000Z",
  "sig": "E1OPiMo40v16KdmyuFR9VWkO8AuWk/WcRYn6u8xpXEc="
}


# Response Type
## API succeed with Data
```
{
  "data": {
    "date": "2026-01-11",
    "temperatureC": 40,
    "temperatureF": 103,
    "summary": "Balmy"
  },
  "message": "SUCCESS",
  "code": "0"
}
```

## API succeed without Data
```
{
  "message": "SUCCESS",
  "code": "0"
}
```

## Handled Error or Problem
```
{
  "type": "urn:problem-type:unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "detail": "無權限執行此API",
  "instance": "/Demo/HandleKnownProblem",
  "code": "unauthorized",
  "requestId": "0HNI96T82U146:00000001",
  "traceId": "00-febac78704dc0db32bf45019e93605c4-12d21d95b749f5ac-00"
}
```

## Unhandled Exception
```
{
  "title": "Unhandled Exception",
  "status": 500,
  "detail": "Attempted to divide by zero.",
  "instance": "/Demo/GetUnknownException",
  "requestId": "0HNI96KITKQN2:00000001",
  "traceId": "00-06de06f1839994e21b283fa2ad47204b-b75958376499b731-00"
}
```
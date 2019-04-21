'use strict';

const express = require('express');

// Constants
const PORT = 8080;
const HOST = '0.0.0.0';

// App
const app = express();
app.get('/', (req, res) => {
  var headerValue = req.get("Permission")

  if (headerValue == "HasSpecialPower") {
    res.send('Hello world\n');
  }

  res.status(403);
  res.send()

});

app.listen(PORT, HOST);
console.log(`Running on http://${HOST}:${PORT}`);
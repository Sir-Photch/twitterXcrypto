from requests import Request, Session
from requests.exceptions import ConnectionError, Timeout, TooManyRedirects
import json

url = 'https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest'
parameters = {
  'start':'1',
  'limit':'50',
  'convert':'USD'
}
headers = {
  'Accepts': 'application/json',
  'X-CMC_PRO_API_KEY': '1d64fe33-4ccc-4b26-824f-ab6d4083ffa5',
}

session = Session()
session.headers.update(headers)

try:
  response = session.get(url, params=parameters)
  data = json.loads(response.text)
  cryptolist = list()
  for elem in data["data"]:
      cryptolist.append(elem["name"])
      cryptolist.append(elem["symbol"])
  print(cryptolist)
except (ConnectionError, Timeout, TooManyRedirects) as e:
  print(e)

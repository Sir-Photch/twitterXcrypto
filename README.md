# twitterXcrypto
.NET 6.0 app initially intended to analyze influencers impact on crypto prices

- configured to run on raspberry-pi as a standalone application with no dependencies
- gets tweets from users on twitter, searches for cryptocurrency-keywords (as well in text as in images that contain text)
- posts these tweets to discord, as well as to a database
- attaches price and -change of mentioned asset to discord post

## how to get started
### build

Load that thang into VisualStudio and run publish with settings:
```xml
<Configuration>Release</Configuration>
<Platform>ARM32</Platform>
<PublishDir>...</PublishDir>
<PublishProtocol>FileSystem</PublishProtocol>
<TargetFramework>net6.0</TargetFramework>
<RuntimeIdentifier>linux-arm</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<PublishTrimmed>true</PublishTrimmed>
```

### configuration
You'll need a resource-file defining environment variables, which are sourced when the app is run. In total, there are:
|Variable name|Contents|
|---|---|
|TWITTER_CONSUMERKEY|Twitter api-keys|
|TWITTER_CONSUMERSECRET||
|TWITTER_ACCESSTOKEN||
|TWITTER_ACCESSSECRET||
|XCMC_PRO_API_KEY|Coinmarketcap api-key|
|NUM_ASSETS_TO_WATCH|Number of crypto-assets to cache|
|DISCORD_WEBHOOK_URL|discord webhook-url|
|USERS_TO_FOLLOW|comma-seperated list with twitter @usernames|
|DATABASE_IP|mysql-database connection details|
|DATABASE_PORT||
|DATABASE_NAME||
|DATABASE_USER||
|DATABASE_PWD||
|TESSERACT_LOCALE|locale for OCR. use `eng` if unsure|
|TESSERACT_WHITELIST|characters to whitelist for OCR|

### running
If for some reason you want to run this _HEAVY-WIP_ project, I recommend using `screen` to detach from it. 
Logs are saved to `/var/log/twitterXcrypto/twixcry.log` on linux. (yeah I know that is not the proper place for user logs. didn't refactor yet.) glhf

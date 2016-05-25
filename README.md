# AmazonEcho-HarmonyBridge
Amazon Echo to Logitech Harmony Bridge

If you just want to use the application get the files from "Compiled Application"
On first run, it will create a settings.xml file, you'll have to fill in the Logitech Harmony credentials and the IP of the server/pc you're running on which you want to use. Afterwards your amazone Echo will be able to find your Harmony Hub and you can say stuff like "Alexa, turn off television" where "television" should be the name of your activity.
Only On and OFF is supported at the moment. But code can easily be extended.



This is C# code that emulates an Hue Bridge, connects to Harmony Hub, enables Amazon Echo to find and use Harmony activities
Runs on Windows and Linux (tested using Mono on a Synology NAS)

It should be easily extended, but will need some serious clean-up :)

Used code/examples from all over the Internet, mostly refered to in comments, some of them:
https://github.com/hdurdle/harmony

https://github.com/armzilla/amazon-echo-ha-bridge

http://www.ag-software.net/agsxmpp-sdk/

And added my own stuff.

First ever GitHub project, let me know if I've done something stupid


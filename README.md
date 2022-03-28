# i18nex.ZipLoader

![2022-03-27 22 34 39](https://user-images.githubusercontent.com/20321215/160284108-18c197d5-42d7-4fc4-ac7d-a0adf47cf3a8.png)  

Script zip support  
Script txt support  
Textures zip support  
Textures png support  
__UI zip not support__  
UI csv support  


# need

i18nEx https://github.com/ghorsington/COM3D2.i18nEx  

# How to use  

COM3D2\i18nEx\loaders\i18nex.ZipLoader.dll  
COM3D2\i18nEx\{lang}\config.ini  

* Create loaders folder in i18nEx folder and put your custom loader DLL into it
* Edit configuration.ini. Set Loader in the [Info] section to the name of the DLL (without the .dll)


# support

- Script  
COM3D2\i18nEx\{lang}\Script\*.zip (sub Directory include)  
COM3D2\i18nEx\{lang}\Script\*.txt (sub Directory include)  

- Textures  
COM3D2\i18nEx\{lang}\Script\*.zip (sub Directory include)  
COM3D2\i18nEx\{lang}\Script\*.png (sub Directory include)  

- UI  
COM3D2\i18nEx\{lang}\Script\{type}\*.csv (sub Directory include)  

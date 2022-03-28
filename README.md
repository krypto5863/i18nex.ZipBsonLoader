# i18nex.ZipLoader

![2022-03-27 22 34 39](https://user-images.githubusercontent.com/20321215/160284108-18c197d5-42d7-4fc4-ac7d-a0adf47cf3a8.png)  

Loader plugin of i18nex plugin

Script zip support  
Script txt support  
Textures zip support  
Textures png support  
__UI zip not support__  
UI csv support  


# need

i18nEx https://github.com/ghorsington/COM3D2.i18nEx  


# How to use  

- easy input  
COM3D2\i18nEx\loaders\i18nex.ZipLoader.dll  
COM3D2\i18nEx\{lang}\config.ini  

- config.ini file  
[Info]  
Loader=i18nex.ZipLoader  

* Create "loaders" folder in "{game folder}\i18nEx" folder and put loader DLL into it  
* Edit "config.ini" Set Loader in the [Info] section to the name of the DLL (without the .dll)  

![2022-03-28 18 10 04](https://user-images.githubusercontent.com/20321215/160365690-da5ae1d1-2a4c-48e1-bc2c-53c0c3af7f69.png)  
![2022-03-28 18 09 44](https://user-images.githubusercontent.com/20321215/160365683-8f185549-f961-4945-a6c1-c8cc2a3a728f.png)  
![2022-03-28 18 12 16](https://user-images.githubusercontent.com/20321215/160366016-6e1c9ea4-4d01-4f6f-8d8c-7ca9dc989d91.png)


# support

- Script  
COM3D2\i18nEx\{lang}\Script\*.zip (sub Directory include)  
COM3D2\i18nEx\{lang}\Script\*.txt (sub Directory include)  

- Textures  
COM3D2\i18nEx\{lang}\Script\*.zip (sub Directory include)  
COM3D2\i18nEx\{lang}\Script\*.png (sub Directory include)  

- UI  
COM3D2\i18nEx\{lang}\Script\{type}\*.csv (sub Directory include)  


# ghorsington's comment

 The way i18nEx handles translation loaders right now is that your custom one will replace the default loader. So basically right now not providing texture loading means textures won't be replaced

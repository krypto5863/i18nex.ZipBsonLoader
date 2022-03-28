# i18nex.ZipLoader

![2022-03-27 22 34 39](https://user-images.githubusercontent.com/20321215/160284108-18c197d5-42d7-4fc4-ac7d-a0adf47cf3a8.png)  

Loader plugin of i18nex plugin

Script zip support  
Script txt support  
Textures zip support  
Textures png support  
UI zip support (Read UI zip, csv Manual)
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
COM3D2\i18nEx\\{lang}\Script\\\*.zip (sub Directory include)  
COM3D2\i18nEx\\{lang}\Script\\\*.txt (sub Directory include)  

- Textures  
COM3D2\i18nEx\\{lang}\Textures\\\*.zip (sub Directory include)  
COM3D2\i18nEx\\{lang}\Textures\\\*.png (sub Directory include)  

- UI  (Read UI zip, csv Manual)
COM3D2\i18nEx\\{lang}\UI\\\*.zip (sub Directory include.)  
COM3D2\i18nEx\\{lang}\UI\\{type}\\\*.csv (sub Directory include. Same as the existing i18nex plug-in method)  
COM3D2\i18nEx\\{lang}\UI\\{type}\\\*.csv (하위 디렉토리 포함. 기존 i18nex 플러그인 방식과 같은)  


## UI zip, csv Manual  

UI csv는 SortedDictionary<string, IEnumerable<string>> 구조로만 처리됩니다.  
UI csv is handled only with SortedDictionary<string, IEnumerable<string>> structures.  

ex) SortedDictionary<"top Dictionary name or zip file name or top Dictionary name in zip", IEnumerable<"csv file name">>  

ex1)
![2022-03-28 19 17 07](https://user-images.githubusercontent.com/20321215/160378005-17d3f601-224c-4506-b117-fd60bbfbb2c4.png)  

ex2)
![2022-03-28 19 18 24](https://user-images.githubusercontent.com/20321215/160378015-93f327c1-ec60-418b-8228-89d81dbfba4c.png)  

ex3)
 ![2022-03-28 19 24 54](https://user-images.githubusercontent.com/20321215/160379038-3e55b50d-6bfc-414f-8631-3b74533a1e8c.png)

 

# ghorsington's comment

 The way i18nEx handles translation loaders right now is that your custom one will replace the default loader.

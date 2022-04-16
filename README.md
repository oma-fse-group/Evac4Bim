# Evac4Bim
A Revit add-in that improves the integration of Fire Evacuation into te BIM worklfow. 

This add-in was developed as part of a master thesis and in partial fulfilment of the requirements for the degree of The International Master of Science in Fire Safety Engineering [(IMFSE)](https://imfse.be/). The full text of thesis will be published shortly. 

This repository hosts the source code and assemblies.

## Revit version
This add-in can run on Revit 2022 or later versions

## Features 
- Extract information required to perform assessments of fire evacuation performance or compliance (e.g number of exits for a space, width of stairways on a storey). 
    The user can also edit/include additional information (occupant profiles, number of occupants, room function/usage, etc.).  
    
    ![image](https://user-images.githubusercontent.com/17513670/163675792-0cbea706-4855-4cb0-9368-fb366d4e250b.png)
 
- Feed the extracted information to evacuation assessment tools such as evacuation simulators (by exporting into an enriched IFC file).  

![image](https://user-images.githubusercontent.com/17513670/163675804-9f347f14-6387-468f-8f31-bb772b0b1957.png)

- Capture results from evacuation assessment tools (such as evacuation simulations or prescrpitive reviews)

- Display assessment results to the user within the Revit interface and store in the model. 

![image](https://user-images.githubusercontent.com/17513670/163675824-033f10ba-096a-4c55-a6d8-c79b44971380.png)

- Animate evacuation simulation results which are provided as time distributions

![image](https://user-images.githubusercontent.com/17513670/163675835-4adb325a-be60-4476-9f3f-8e69f3eccc53.png)

- Generate enriched IFC files supporting the fire evacuation data requirements of the new draft MVD for FSE. 

![image](https://user-images.githubusercontent.com/17513670/163675843-3f28a7cc-138e-420b-bf88-140e5182cf9e.png)

- Perform automated prescription reviews based on the IBC code.   

![image](https://user-images.githubusercontent.com/17513670/163675854-c9a7e6be-8e64-4a0c-8b33-82c2d2ed3047.png)

## Installation Instructions 
- Download the latest release from [here](https://github.com/YakNazim/Evac4Bim/releases)
- Unpack the content of the zip package into Revit's addin folder
e.g C:\ProgramData\Autodesk\Revit\Addins\2022

![image](https://user-images.githubusercontent.com/17513670/163676617-a89b5c66-236f-47f1-b96f-030662964981.png)

- Launch Revit

![image](https://user-images.githubusercontent.com/17513670/163677281-232b79d1-bda8-4676-b659-e7dbda7b0d75.png)


## Build Instructions 
If you wish to build the source code, you can clone/download the repository. 

**Note**: The source code for the extended IFC exporter is hosted on a different repository [(Revit-IFC-Master)](https://github.com/YakNazim/Revit-IFC-Master) and has to be built separarelty then deployed along with this add-in. However, the release version (presented in the previous section) includes all the assemblies in one.  

- Open the project solution (Evac4Bim.sln) in Visual Studio
The solution and its sub projects are already configured but ensure your local environment meets the following requirements before attempting to build: 

>   - .NET FRAMEWORK version 4.8 in Visual Studio
>
>   - A local installation of Autodesk Revit (since the solution references several dll files from it) 
>
>   - In the project solution, ensure the path to each referenced dll is correct (do this for every sub project)
>
>![Capture d’écran 2022-04-16 162949](https://user-images.githubusercontent.com/17513670/163678998-a78af818-25ef-409e-8a3d-ca9640ff2a17.png)
>
>   - If the target path does not match the local installation folder of Revit, you can add the reference manually and overwrite the previous one.   
>
>![Picture1](https://user-images.githubusercontent.com/17513670/163679735-1c87aed4-0099-4167-a219-0dc156930e13.png)
>
>   - In addition to that, you need to install the following dependencies from NuGet
>
>![image](https://user-images.githubusercontent.com/17513670/163678225-88e04772-b575-4024-888a-1955b34cce75.png)
>
- Once you setup the environment, build the solution 
- To deploy the solution, copy the assemblies into Revit's addin folder e.g C:\ProgramData\Autodesk\Revit\Addins\2022

**Note** : the project solution is configured to autmatically copy the assemblies and dependencies into the add-in folder as a "post-build" event. You can adjust the path to the folder in project properties or disable the post-build event. 

**Note 2** : In any case, you have to follow the same folder structure as the install package provided for installation (see above). 

- Do not forget to add manifest files (.addin). You can use the manifest files included in the release as template

## Case Studies and Sample Projects 
You can find sample project files on this [repo](https://mega.nz/folder/TPpyjAQC#VJr5T6PZo0-9qF5yHBNvPw). 
It includes Revit models, IFC files, Pathfinder models as well as the Solibri ruleset used for validation. Additionally, video recordings are included. 

## Known Limitations 
The following limitations should be considered regarding the prototype code reviewer: 
-	Only starting number of occupants is considered for evacuation capacity check (i.e., rooms discharging to another room are not considered) 
-	Path of travel: only one per room. Alternative paths are not considered.
-	There can only be one discharge level in a multistorey building. At the discharge level, discharge exits are sized for all occupants from all other storeys (including the discharge level itself). 
-	Spiral stairways not considered 
-	Stair landing path is computed following the Predtechenskii and Milinskii method. 
-	Refuge areas are not considered. 
-	Project units should be set in SI units.
-	Only gross area is considered. Net area is not deducted.
-	High rise buildings are not considered.  
-	Prescriptions related to corridors are not checked.  
-	Doors must be oriented outwards.  

## Code reference
An overview of the code is given in [this wiki](https://github.com/YakNazim/Evac4Bim/wiki/Code-reference)

## Acknowledgments

- Dr Pete Thompson and Dr Enrico Ronchi (Lund University) : for their support throughout my thesis work and for introducing me into this research topic. 

- Jimmy Abualdenien (TU München) and Dr. Asim Siddiqui (University of Greenwich) : for their guidance and research on the Fire Safety Engineering MVD for BIM. 

- [Thunderhead Engineering](https://www.thunderheadeng.com/) : for their technical support and input on working with the Pathfinder and for developing a demo version which supports the draft IFC schema for fire evacuation. 

- The IMFSE consortium  : for allowing me to pursue this MSc in Fire Safety Engineering and the financial support which made this thesis possible. 

- IFC for Revit and Navisworks (revit-ifc) by Autodesk [(available on Github)](https://github.com/Autodesk/revit-ifc)

- Icons from https://www.flaticon.com/ 

## Revit-IFC-Master
This is a fork of the open source Revit IFC exporter allowing to define custom Ifc properties. It can be found on this repository : [Revit-IFC-Master](https://github.com/YakNazim/Revit-IFC-Master) 


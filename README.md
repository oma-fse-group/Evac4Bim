# Evac4Bim
A Revit add-in for better integration of Fire Evacuation into te BIM worklfow
This repository hosts the source code and assemblies.

## Revit version
This add-in can run on Revit 2022 or later versions

## Features 
1.	Extract information required to perform assessments of fire evacuation performance or compliance (e.g number of exits for a space, width of stairways on a storey). 
    The user can also edit/include additional information (occupant profiles, number of occupants, room function/usage, etc.).  
    
    ![image](https://user-images.githubusercontent.com/17513670/163675792-0cbea706-4855-4cb0-9368-fb366d4e250b.png)
 
2. Feed the extracted information to evacuation assessment tools such as evacuation simulators (by exporting into an enriched IFC file).  

![image](https://user-images.githubusercontent.com/17513670/163675804-9f347f14-6387-468f-8f31-bb772b0b1957.png)

3. Capture results from evacuation assessment tools (such as evacuation simulations or prescrpitive reviews)

5. Display assessment results to the user within the Revit interface and store in the model. 

![image](https://user-images.githubusercontent.com/17513670/163675824-033f10ba-096a-4c55-a6d8-c79b44971380.png)

5. Animate evacuation simulation results which are provided as time distributions

![image](https://user-images.githubusercontent.com/17513670/163675835-4adb325a-be60-4476-9f3f-8e69f3eccc53.png)

6. Generate enriched IFC files supporting the fire evacuation data requirements of the new draft MVD for FSE. 

![image](https://user-images.githubusercontent.com/17513670/163675843-3f28a7cc-138e-420b-bf88-140e5182cf9e.png)

7. Perform automated prescription reviews based on the IBC code.   

![image](https://user-images.githubusercontent.com/17513670/163675854-c9a7e6be-8e64-4a0c-8b33-82c2d2ed3047.png)

## Installation Instructions 
- Download the latest release from [here](https://github.com/YakNazim/Evac4Bim/releases)
- Unpack the content of the zip package into Revit's addin folder
e.g C:\ProgramData\Autodesk\Revit\Addins\2022

![image](https://user-images.githubusercontent.com/17513670/163676617-a89b5c66-236f-47f1-b96f-030662964981.png)

## Build Instructions 

## Case Studies and Sample Projects 
You can find sample project files on this [repo](https://mega.nz/folder/TPpyjAQC#VJr5T6PZo0-9qF5yHBNvPw). 
It includes Revit models, IFC files, Pathfinder models as well as the Solibri ruleset used for validation. Additionally, video recordings are included. 

## Known Limitations 

## Acknowledgment

- Icons from https://www.flaticon.com/ 
- Revit ifc export 

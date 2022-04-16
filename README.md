# Evac4Bim
A Revit add-in for better integration of Fire Evacuation into te BIM worklfow. 

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

## Case Studies and Sample Projects 
You can find sample project files on this [repo](https://mega.nz/folder/TPpyjAQC#VJr5T6PZo0-9qF5yHBNvPw). 
It includes Revit models, IFC files, Pathfinder models as well as the Solibri ruleset used for validation. Additionally, video recordings are included. 

## Known Limitations 

## Acknowledgments

- Dr Pete Thompson and Dr Enrico Ronchi (Lund University) : for their support throughout my thesis work and for introducing me into this research topic. 

- Jimmy Abualdenien (TU München) and Dr. Asim Siddiqui (University of Greenwich) : for their guidance and research on the Fire Safety Engineering MVD for BIM. 

- [Thunderhead Engineering](https://www.thunderheadeng.com/) : for their technical support and input on working with the Pathfinder and for developing a demo version which supports the draft IFC schema for fire evacuation. 

- The IMFSE consortium  : for allowing me to pursue this MSc in Fire Safety Engineering and the financial support which made this thesis possible. 

- IFC for Revit and Navisworks (revit-ifc) by Autodesk [(available on Github)](https://github.com/Autodesk/revit-ifc)

- Icons from https://www.flaticon.com/ 


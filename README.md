# ConFormSim
_An expansion of Unity ML-Agents, made to support concept formation research and
faster prototyping._

---

<img alt="logo" align="right" height="175px" src="docs/images/Robot_Pose.png" style="padding-left:20px">

**ConFormSim** is an open-source extension for the [Unity ML-Agents
Toolkit](https://github.com/Unity-Technologies/ml-agents). It offers useful
features on the Unity site, to allow an easy and fast prototyping. The focus is
on improvements that facilitate a research in concept formation. Therefore,
ConFormSim provides an easy configurable and modular property system, which
allows more abstract and multimodal perceptions. A new ActionProvider component
allows modular action sets to test different action abstraction levels and
switch easily between them. These features allow to make a task and environment
more symbolic (abstract levels) or subsymbolic (distributed, control problems).
This supports research on the formation of relational representations, knowledge
transfer, generalization and data efficiency. 


## Features

- Flexible, modular action system configurable from Unity Editor GUI
- modular and configurable object property system
- new ML-Agents compatible sensors for the object property system
- integration of in new sensors perception functions like sensor noise 
- environment randomization and data augmentation with "instance noise"
- 2 example environments

## Documentation
### Installation and Set-up
- [Installation](docs/Installation.md)
### Getting Started
- [Getting Started](docs/Getting_started.md)
- [Object Property System](docs/ObjectPropertySystem.md)
- [Action System](docs/ActionSystem.md)
- [Example Environments]()
  - [Storage Environment](docs/StorageEnv.md)
  - [Boxworld Environment](docs/BoxWorld.md) 

## License
[MIT License](LICENSE)





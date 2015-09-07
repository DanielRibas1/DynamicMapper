# CodeDom Dynamic Mapper

# 1.0 Project Description
The finality of this project is to provide a solution for mapping large amount of entities across different layers, but these entities aren't shared throught the solution. It's focused on solutions that are still using .Net Framework 3.5.
The mapping method builds and compile the mapper code on run-time, after it's built it loads the output assembly by Lazy loading, and make accesible the mapper class. The mapper maker only need to build the needed classes and mehtods for a pair of entities one time, the second time and further it will get from a store object, so the performance time at second executions is near 0.
The mappers are thread safe, so you can use it on a multi-threaded solution or into Parallel blocks.

At this time the following mapping features are implemented:
- Symetric classes
- Any object to string
- All primitive castable types (short to int, etc)
- Parseable value to Enum
- Complex inner classes
- Arrays & IList object types of primitive or custom object

# 1.1 Configuration & Installation
At this moment the installation is just add as reference at your project.
At this moment there aren't configuration, may be in the future
No external references or assemblies needed.
Works with .Net Framework 3.5 and above. (Tested with 4.5 also)

# 1.2 Use
For use the mapping just need to invoke the Map method of MapperManager static object with the desired types as generic parameters. Example:
var result = Dynamic.MapperManager.Map<OriginDTO, DestinationDTO>(dto);

# 1.3 Licence
Basic MIT Licence. See LICENCE File into the project for more information.

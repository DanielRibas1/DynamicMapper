# CodeDom Dynamic Mapper

The finality of this project is to provide a solution for mapping large amount of entities across different layers, but these entities aren't shared throught the solution. It's focused on solutions that are still using .Net Framework 3.5.
The mapping method builds and compile the mapper code on run-time, after it's built it loads the output assembly by Lazy loading, and make accesible the mapper class. The mapper maker only need to build the needed classes and mehtods for a pair of entities one time, the second time and further it will get from a store object, so the performance time at second executions is near 0.
The mappers are thread safe, so you can use it on a multi-threaded solution or into Parallel blocks.

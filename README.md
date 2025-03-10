# LiellaNG: Lightweight Intermediate-code transform Engine for Low-Level Assembling Next Generation

Liella is an AOT compiler designed for transforming Microsoft CLI Intermediate code into highly-optimized native code. 
Liella is oriented to low-level scenarios like system development, providing direct control to instruction-level  
operations as well as advanced features such as object-oriented programming, generic types and functional features.

🚧 Development is in progress. 

-----------------

## Progress

### Basic

- [x] Metadata resoution
- [x] Non generic types/method collection and pruning
- [x] On-demand generic type instantiation
- [x] Infinity instantiation detection
- [x] IL decoding
- [x] Basic blocks and CFG building
- [x] Auto optimized field layout
- [x] Stack balance analysis
- [x] Stack machine dataflow analysis
- [x] Virtual table generation
- [ ] Intermediate representation generation
- [ ] Full object system
- [ ] Embedded linker
- [ ] Allocation/Garbage collection
- [ ] Exception system
- [ ] Escape analysis
- [ ] Type-based devirtualization/code pruning

### Basic Language features

- [ ] Static methods
- [ ] PInvoke

- [ ] Function pointers
- [ ] Variable-length arguments
- [ ] Linear allocator
- [ ] String literals
- [ ] Instance field access
- [ ] Static field access
- [ ] Basic polymorphism
- [ ] Interface cast
- [ ] Dynamic dispatch on interfaces
- [ ] Struct fine-grain layout control
- [ ] Box
- [ ] Struct methods
- [ ] Struct dynamic dispatch
- [ ] On-stack allocation
- [ ] Delegate
- [ ] Lambda expressions and closures

### Extension features
- [ ] Goto label by address expression
- [ ] Inline assembly 
- [ ] User defined allocation routines
- [ ] Explicit no-allocation regions
- [ ] Incremental compilation
- [ ] Pauseless GC
- [ ] Reflection system
- [ ] Explicit compile-time evaluation
- [ ] Vector instructions support
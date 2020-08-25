# GrpcServiceProvider
A simple way to make GRPC service from ordinary .Net class

## Limitations
This requires Grpc.tools package to work
Only simple classes can be exposed as service
Each public method of target class should have return class or return List<T>
Target class must have parameterless constructor
Only unary calls are supported
  
## Usage 
1) Create instance of provider class:
```
//in this example grpc tools is located in the working directory of application
GrpcService.GrpcServiceProvider provider = new GrpcService.GrpcServiceProvider("protoc.exe","grpc_csharp_plugin.exe");
```
2) in UseEndpoints block of Asp/Kestrel configuration add line:
```
//In this case  we are exposing SomeClass class as service
provider.MapEndpoint<SomeClass>(endpoints, this);
```
3) That`s it! Now you can grab a proto file from Protos folder in the working directory of application and make GRPC calls directly to your class

## Example

You can build and run example from repo

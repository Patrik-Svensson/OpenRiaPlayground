These sample projects show the following:
* Using different platforms with the same server 
** both using a shared portable client library (The recommended approach?) and without
* Using async await
* Building a custom domainclient (OpenRiaServices.DomainServices.PortableWeb) and making sure it is used

IMPORTANT:


\packages\OpenRiaServices.Client.Core.4.4.0.3 is not the standard nuget package but a modified version based on
https://openriaservices.codeplex.com/SourceControl/network/forks/danneesset/openriaservices?branch=domainclientfactory

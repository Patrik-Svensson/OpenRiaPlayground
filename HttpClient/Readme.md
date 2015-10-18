# Readme HttpClient 

## Porpose of solution:

* Building a custom domainclient (OpenRiaServices.DomainServices.PortableWeb) that will hopefylle later be incorporated into OpenRiaServices

## IMPORTANT:


1. \packages\OpenRiaServices.Client.Core.4.4.0.3 is not the standard nuget package but a modified version based on
https://openriaservices.codeplex.com/SourceControl/network/forks/danneesset/openriaservices?branch=domainclientfactory

2. The client URI for non-SL clients are configured so that you must install and run Fiddler (http://www.telerik.com/fiddler) while debugging 
(they use localhost.fiddler instead of just localhost).

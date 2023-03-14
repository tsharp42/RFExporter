# RFExporter
Tool for extracting high resolution scan data from an RFExplorer for use with WSM/WWB

## RFExplorerNET.RFExplorerCommunicator
A quick port of a subset of the [RFExplorer-for-.NET](https://github.com/RFExplorer/RFExplorer-for-.NET) RFExplorerCommunicator library to .NET 7.0 - Cross platform compatibility has not been tested yet.

## RFExporter.Data.ScanningService
Library to encapsulate the logic for scanning block-by-block using the RFExplorerCommunicator

## RFExporter.UI
MAUI based user interface for the the RFExporter.Data.ScanningService library

## RFExporter.CommandLine
Text based user interface for the the RFExporter.Data.ScanningService library

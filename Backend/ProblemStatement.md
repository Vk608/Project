 Assignment: PubMed Data Sync Validation POC 
Business Problem 

Our internal editorial system stores article metadata that may become out of sync with the official PubMed dataset. Build a solution to identify such discrepancies. 

Part 1: FAB 

Objective 

Use FAB to index PubMed data and create a search agent. 

Tasks 

Download sample XML files from PubMed dataset (To start with, use fewer files) 
Index data using FAB 
Create a search agent in FAB with the following capabilities: 
Search by any combination of parameters: 
PubMed ID 
Abstract 
Journal Name 
Publication Year 
Authors 
Keywords 
Return confidence score for matches 
Support both exact lookup (by PubMed ID) and semantic search (by other params) 
Deliverables 

For below deliverables create a design and impact statement document 1st and get it reviewed
 Indexed PubMed dataset in FAB 
 A search agent with multi-parameter support 
 Confidence scoring implementation 
Part 2: .NET Background Utility 

Objective 

Create a .NET utility that validates internal system data against PubMed via the FAB agent. 

Tasks 

Create a mock json file with the citation data. 
Call the FAB agent to validate records 
Identify records where PubMed ID no longer exists 
Compare fields and flag mismatches 
Store validation results 
Deliverables 

For below deliverables create a design and impact statement document 1st and get it reviewed

 .NET utility 
 FAB agent integration 
 Validation and flagging logic 
 

Part 3: Angular UI 

Objective 

Build an Angular application to display validation results. 

Tasks 

Create a dashboard to display validation report 
Show flagged records with discrepancy details 
Display confidence scores 
Filter/search functionality 
Deliverables 

For below deliverables create a design and impact statement document 1st and get it reviewed

 Angular application 
 Validation report dashboard 
 Interactive UI for reviewing results 
 

Summary 

Part 

Tool 

Responsibility 

Part 1 

FAB 

Index PubMed data + Create search agent (multi-param, confidence scoring) 

Part 2 

.NET 

Validate internal data → Flag discrepancies 

Part 3 

Angular 

Display validation report UI 
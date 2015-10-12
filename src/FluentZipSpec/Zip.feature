Feature: Zip and unzip
	In order to zip and unzip files and binary contents
	As a developer
	I want to use a fluent API

Scenario: Zip and unzip a folder
	Given a clean test directory
	When I zip . into ..\archive.zip
	And I unzip ..\archive.zip into ..\unzip
	Then the contents of . should be identical to the contents of ..\unzip
	And ..\archive.zip should exist

Scenario: Zip and unzip in memory
	When I zip "zipped contents" in memory as foo\bar.txt
	Then the content of the in-memory zip is foo\bar.txt:"zipped contents"
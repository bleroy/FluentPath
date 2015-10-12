Feature: File and directory creation
	In order to create files and directories
	As a developer
	I want to use fluent APIs

Scenario: Create directory
	Given a clean test directory
	When I get the path for newdir
	And I create that directory 
	Then there should be a newdir directory

Scenario: Create directory from path
	Given a clean test directory
	When I create a directory from newdir\newsubdir
	Then there should be a newdir\newsubdir directory

Scenario: Create subdirectory from path
	Given a clean test directory
	When I create a newsubdir subdirectory from bar\bar
	Then there should be a bar\bar\newsubdir directory

Scenario: Create a collection of directories
	Given a clean test directory
	When I use a Lambda to create directories with the same names as file in bar
	Then the resulting set should be baz, notes, deep
	And the content of . should be foo.txt, bar, baz, notes, deep, sub

Scenario: Create a text file with default encoding
	Given a clean test directory
	When I create a bar\dejavu.txt text file with the text "Déjà Vu"
	Then the binary content of bar\dejavu.txt should be 44c3a96ac3a0205675

Scenario: Create a text file with UTF-16 encoding
	Given a clean test directory
	When I create a UTF-16 encoded bar\dejavu.txt file with the text "Déjà Vu"
	Then the binary content of bar\dejavu.txt should be fffe4400e9006a00e000200056007500

Scenario: Create a binary file
	Given a clean test directory
	When I create a bar\binary.bin binary file with 010045f974123456
	Then the binary content of bar\binary.bin should be 010045f974123456

Scenario: Create directories with the same name under a collection of directories
	Given a clean test directory
	When I create newdir subdirectories under ., bar, bar\bar
	Then newdir should exist
	And bar\newdir should exist
	And bar\bar\newdir should exist
Feature: Path Exploration
	In order to select a set of file system entries
	As a developer
	I want to explore the file system using a fluent API

Scenario: Find all files
	Given a clean test directory
	When I select all files
	Then the resulting set should be bar\bar\deep.txt, bar\baz.txt, bar\notes.txt, foo.txt, sub\baz.txt, sub\binary.bin

Scenario: Find all text files in a directory
	Given a clean test directory
	When I recursively select *.txt
	Then the resulting set should be bar\bar\deep.txt, bar\baz.txt, bar\notes.txt, foo.txt, sub\baz.txt

Scenario: Find subdirectories
	Given a clean test directory
	When I select subdirectories
	Then the resulting set should be bar, sub

Scenario: Find all subdirectories
	Given a clean test directory
	When I select deep subdirectories
	Then the resulting set should be bar, bar\bar, sub, sub\subsub

Scenario: Find subdirectories with a pattern
	Given a clean test directory
	When I search for subdirectories with pattern sub*
	Then the resulting set should be sub, sub\subsub

Scenario: Find subdirectories with a condition
	Given a clean test directory
	When I search for subdirectories with a condition
	Then the resulting set should be sub

Scenario: Find all subdirectories with a condition
	Given a clean test directory
	When I search for deep subdirectories with a condition
	Then the resulting set should be sub, sub\subsub

Scenario: Find files
	Given a clean test directory
	When I select files
	Then the resulting set should be foo.txt

Scenario: Find files with a condition
	Given a clean test directory
	When I search for files with a condition
	Then the resulting set should be foo.txt

Scenario: Find all files with a condition
	Given a clean test directory
	When I search for deep files with a condition
	Then the resulting set should be bar\bar\deep.txt, bar\baz.txt, bar\notes.txt, foo.txt, sub\baz.txt

Scenario: Find all files with a pattern
	Given a clean test directory
	When I search for deep files with pattern *.txt
	Then the resulting set should be bar\bar\deep.txt, bar\baz.txt, bar\notes.txt, foo.txt, sub\baz.txt

Scenario: Find files with specific extensions
	Given a clean test directory
	When I select files with extensions bin, foo
	Then the resulting set should be sub\binary.bin

Scenario: Find file system entries
	Given a clean test directory
	When I select file system entries
	Then the resulting set should be bar, foo.txt, sub

Scenario: Find file system entries with a condition
	Given a clean test directory
	When I search for file system entries with a condition
	Then the resulting set should be bar

Scenario: Find all file system entries with a condition
	Given a clean test directory
	When I search for deep file system entries with a condition
	Then the resulting set should be bar, bar\bar, bar\baz.txt, sub\baz.txt, sub\binary.bin

Scenario: Find all file system entries with a pattern
	Given a clean test directory
	When I search for deep file system entries with pattern *a*
	Then the resulting set should be bar, bar\bar, bar\baz.txt, sub\baz.txt, sub\binary.bin

Scenario: Directories and files exist
	Given a clean test directory
	Then foo.txt should exist
	And bar should exist
	And bar\baz.txt should exist
	And bar\notes.txt should exist
	And bar\bar should exist
	And bar\bar\deep.txt should exist
	And sub should exist
	And sub\subsub should exist
	And sub\binary.bin should exist
	And unknown should not exist
	And bar\unknown should not exist
	And unknown\unknown should not exist

Scenario: Enumerate directories twice
	Given a clean test directory
	When I enumerate directories twice
	Then the resulting string should be "bar, bar, sub, sub"
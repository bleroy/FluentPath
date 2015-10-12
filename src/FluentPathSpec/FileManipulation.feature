Feature: File Manipulation
	In order to modify files
	As a developer
	I want to use a fluent API

Scenario: Change extension
	Given a clean test directory
	When I change the extension of bar\notes.txt to foo
	Then bar\notes.txt should not exist
	And bar\notes.foo should exist

Scenario: Delete a file
	Given a clean test directory
	When I delete file bar\notes.txt
	Then bar\notes.txt should not exist

Scenario: Delete a non existing file
	Given a clean test directory
	When I delete file bar\thisfiledoesnotexist.txt
	Then bar\thisfiledoesnotexist.txt should not exist

Scenario: Delete a directory
	Given a clean test directory
	When I delete directory bar
	Then bar should not exist
	And bar\baz.txt should not exist
	And bar\notes.txt should not exist
	And bar\bar should not exist
	And bar\bar\deep.txt should not exist

Scenario: Process a text file
	Given a clean test directory
	When I process the path and content of bar\baz.txt
	Then the text content of bar\baz.txt should be "BAR BAZ - processed baz.txt"

Scenario: Process the content of a text file
	Given a clean test directory
	When I process the content of bar\baz.txt
	Then the text content of bar\baz.txt should be "BAR BAZ - processed"

Scenario: Binary process the content of a file
	Given a clean test directory
	When I binary process the content of sub\binary.bin
	Then the binary content of sub\binary.bin should be fffefdfcfbfa00

Scenario: Binary process the path and content of a file
	Given a clean test directory
	When I binary process the path and content of sub\binary.bin
	Then the binary content of sub\binary.bin should be fffefdfcfbfa00
	And the resulting string should be "binary.bin"
	
Scenario: Replace the text of a file
	Given a clean test directory
	When I replace the text of \bar\baz.txt with "replaced"
	Then the text content of bar\baz.txt should be "replaced"

Scenario: Grep files
	Given a clean test directory
	When I grep for "\stext\s"
	Then the resulting string should be "foo.txt:9, bar\notes.txt:9"

Scenario: Grep a single file
	Given a clean test directory
	When I grep for "is" in bar\notes.txt
	Then the resulting string should be "2, 5"

Scenario: Add permissions to a file
	Given a clean test directory
	When I add a permission to \bar\baz.txt
	Then there is an additional permission on \bar\baz.txt

Scenario: Add permissions to a directory
	Given a clean test directory
	When I add a permission to \bar\bar
	Then there is an additional permission on \bar\bar

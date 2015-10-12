Feature: File copy
	In order to copy parts of the file system
	As a developer
	I want to call file copy APIs

Scenario: Single file copy to directory
	Given a clean test directory
	When I copy foo.txt to sub
	Then the content of sub should be subsub, baz.txt, binary.bin, foo.txt
	And foo.txt should exist

Scenario: Single file copy
	Given a clean test directory
	When I copy foo.txt to sub\foocopy.txt
	Then the content of sub should be subsub, baz.txt, binary.bin, foocopy.txt
	And foo.txt should exist

Scenario: File copy with overwrite always
	Given a clean test directory
	When I overwrite always copy with bar\baz.txt to sub
	Then the text content of bar\baz.txt should be "bar baz"
	And the text content of sub\baz.txt should be "bar baz"

Scenario: File copy with overwrite never
	Given a clean test directory
	When I overwrite never copy with bar\baz.txt to sub
	Then the text content of bar\baz.txt should be "bar baz"
	And the text content of sub\baz.txt should be "sub baz"

Scenario: Copy older file with overwrite if newer
	Given a clean test directory
	When I overwrite ifnewer copy with bar\baz.txt to sub
	Then the text content of bar\baz.txt should be "bar baz"
	And the text content of sub\baz.txt should be "sub baz"

Scenario: Copy newer file with overwrite if newer
	Given a clean test directory
	When I overwrite ifnewer copy with sub\baz.txt to bar
	Then the text content of bar\baz.txt should be "sub baz"
	And the text content of sub\baz.txt should be "sub baz"

Scenario: File copy with overwrite throw
	Given a clean test directory
	When I overwrite throw copy with bar\baz.txt to sub
	Then InvalidOperationException should be thrown

Scenario: Copy single file with transform
	Given a clean test directory
	When I copy foo.txt with a transform
	And I select files
	Then the resulting set should be foo.txt, foofoo.txt

Scenario: Shallow directory copy
	Given a clean test directory
	When I copy bar to sub
	Then the content of sub should be subsub, baz.txt, binary.bin, notes.txt
	And sub\bar should not exist
	And the content of bar should be bar, baz.txt, notes.txt

Scenario: Deep directory copy
	Given a clean test directory
	When I recursively copy bar to sub
	Then the content of sub should be bar, subsub, baz.txt, binary.bin, notes.txt
	And the content of sub\bar should be deep.txt

Scenario: Single file move to directory
	Given a clean test directory
	When I move foo.txt to sub
	Then the content of sub should be subsub, baz.txt, binary.bin, foo.txt
	And foo.txt should not exist

Scenario: Single file move
	Given a clean test directory
	When I move foo.txt to sub\foocopy.txt
	Then the content of sub should be subsub, baz.txt, binary.bin, foocopy.txt
	And foo.txt should not exist

Scenario: File move with overwrite always
	Given a clean test directory
	When I overwrite always move with bar\baz.txt to sub
	Then bar\baz.txt should not exist
	And the text content of sub\baz.txt should be "bar baz"

Scenario: File move with overwrite never
	Given a clean test directory
	When I overwrite never move with bar\baz.txt to sub
	Then the text content of bar\baz.txt should be "bar baz"
	And the text content of sub\baz.txt should be "sub baz"

Scenario: Move older file with overwrite if newer
	Given a clean test directory
	When I overwrite ifnewer move with bar\baz.txt to sub
	Then the text content of bar\baz.txt should be "bar baz"
	And the text content of sub\baz.txt should be "sub baz"

Scenario: Move newer file with overwrite if newer
	Given a clean test directory
	When I overwrite ifnewer move with sub\baz.txt to bar
	Then sub\baz.txt should not exist
	And the text content of bar\baz.txt should be "sub baz"

Scenario: File move with overwrite throw
	Given a clean test directory
	When I overwrite throw move with bar\baz.txt to sub
	Then InvalidOperationException should be thrown

Scenario: Move single file with transform
	Given a clean test directory
	When I move foo.txt with a transform
	And I select files
	Then the resulting set should be foofoo.txt

Scenario: Directory move
	Given a clean test directory
	When I move bar to sub
	Then the content of sub should be bar, subsub, baz.txt, binary.bin, notes.txt
	And sub\bar should exist
	And the content of sub\bar should be deep.txt
	And the text content of bar\baz.txt should be "bar baz"
	And the text content of sub\baz.txt should be "sub baz"

Scenario: Collection copy
	Given a clean test directory
	When I use a Lambda to copy text files into copy
	Then the resulting set should be copy\foo.txt, copy\baz.txt, copy\notes.txt, copy\deep.txt
	And the content of copy should be foo.txt, baz.txt, notes.txt, deep.txt
	And the content of . should be foo.txt, bar, sub, copy
	
Scenario: Collection move
	Given a clean test directory
	When I use a Lambda to move text files into moved
	Then the resulting set should be moved\foo.txt, moved\baz.txt, moved\notes.txt, moved\deep.txt
	And the content of moved should be foo.txt, baz.txt, notes.txt, deep.txt
	And the content of . should be bar, sub, moved
	And the content of bar should be bar
	And the content of sub should be binary.bin, subsub
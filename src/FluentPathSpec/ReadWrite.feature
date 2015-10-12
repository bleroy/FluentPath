Feature: Read and write files
	In order to read and write files in various encodings
	As a developer
	I want to use a fluent API

Scenario: Write to a text file with default encoding
	Given a clean test directory
	When I create a bar\dejavu.txt text file with the text "Déjà Vu"
	Then the binary content of bar\dejavu.txt should be 44c3a96ac3a0205675

Scenario: Write to a text file with UTF-16 encoding
	Given a clean test directory
	When I create a UTF-16 encoded bar\dejavu.txt file with the text "Déjà Vu"
	Then the binary content of bar\dejavu.txt should be fffe4400e9006a00e000200056007500

Scenario: Write to a binary file
	Given a clean test directory
	When I create a bar\binary.bin binary file with 010045f974123456
	Then the binary content of bar\binary.bin should be 010045f974123456

Scenario: Read a text file
	Given a clean test directory
	Then the text content of bar\baz.txt should be "bar baz"
	
Scenario: Read a binary file
	Given a clean test directory
	Then the binary content of sub\binary.bin should be 000102030405ff

Scenario: Open a text file
	Given a clean test directory
	When I open bar\baz.txt
	Then the resulting string should be "bar baz"

Scenario: Open files
	Given a clean test directory
	When I open bar\baz.txt, sub\baz.txt and read the contents
	Then the resulting string should be "bar bazsub baz" 
	
Scenario: Open files with path
	Given a clean test directory
	When I open bar\baz.txt, sub\baz.txt and read the path and contents
	Then the resulting string should be "bar\baz.txt:bar bazsub\baz.txt:sub baz"

Scenario: Read files
	Given a clean test directory
	When I read the contents of bar\baz.txt, sub\baz.txt
	Then the resulting string should be "bar bazsub baz"
	
Scenario: Read files with path
	Given a clean test directory
	When I read the path and contents of bar\baz.txt, sub\baz.txt
	Then the resulting string should be "bar\baz.txt:bar bazsub\baz.txt:sub baz"
	
Scenario: Append text to a file
	Given a clean test directory
	When I append " - this was appended" to \bar\baz.txt
	Then the text content of bar\baz.txt should be "bar baz - this was appended"

Scenario: Writing then reading a file with UTF-16 encoding
	Given a clean test directory
	When I create a UTF-16 encoded bar\dejavu.txt file with the text "Déjà Vu"
	Then the text content of bar\dejavu.txt as read using UTF-16 encoding should be "Déjà Vu"

Scenario: Writing to a file with encoding UTF-16 replaces the contents
	Given a clean test directory
	When I write "Déjà Vu" to bar\baz.txt using UTF-16 encoding
	Then the text content of bar\baz.txt as read using UTF-16 encoding should be "Déjà Vu"

Scenario: Binary writing to a file
	Given a clean test directory
	When I write bytes 44c3a96ac3a0205675 to bar\baz.txt
	Then the text content of bar\baz.txt should be "Déjà Vu"

Scenario: Appending to a file with encoding UTF-16 appends new contents
	Given a clean test directory
	When I create a UTF-16 encoded bar\dejavu.txt file with the text "Déjà Vu"
	And I append " Déjà Vu" to bar\dejavu.txt using UTF-16 encoding
	Then the text content of bar\dejavu.txt as read using UTF-16 encoding should be "Déjà Vu Déjà Vu"

Scenario: Encrypting and decrypting a file
	Given a clean test directory
	When I encrypt bar\baz.txt
	And I copy bar\baz.txt to bar\encrypted.txt
	And I decrypt bar\baz.txt
	Then the text content of bar\baz.txt should be "bar baz"
	And the text content of bar\encrypted.txt should be "bar baz"
    And bar\baz.txt should not be encrypted
	And bar\encrypted.txt should be encrypted

Scenario: Setting attributes
	Given a clean test directory
	When I set attributes Temporary, Hidden on bar\baz.txt
	Then attributes Temporary, Hidden should be set on bar\baz.txt

Scenario: Setting creation time
	Given a clean test directory
	When I set creation time on bar\baz.txt to 5/21/1970
	And I set creation time on bar\bar to 5/21/1970
	Then the creation time on bar\baz.txt is 5/21/1970
	And the creation time on bar\bar is 5/21/1970
	
Scenario: Setting UTC creation time
	Given a clean test directory
	When I set UTC creation time on bar\baz.txt to 5/21/1970
	And I set UTC creation time on bar\bar to 5/21/1970
	Then the UTC creation time on bar\baz.txt is 5/21/1970
	And the UTC creation time on bar\bar is 5/21/1970
	
Scenario: Setting last access time
	Given a clean test directory
	When I set last access time on bar\baz.txt to 5/21/1970
	And I set last access time on bar\bar to 5/21/1970
	Then the last access time on bar\baz.txt is 5/21/1970
	And the last access time on bar\bar is 5/21/1970
	
Scenario: Setting UTC last access time
	Given a clean test directory
	When I set UTC last access time on bar\baz.txt to 5/21/1970
	And I set UTC last access time on bar\bar to 5/21/1970
	Then the UTC last access time on bar\baz.txt is 5/21/1970
	And the UTC last access time on bar\bar is 5/21/1970
	
Scenario: Setting last write time
	Given a clean test directory
	When I set last write time on bar\baz.txt to 5/21/1970
	And I set last write time on bar\bar to 5/21/1970
	Then the last write time on bar\baz.txt is 5/21/1970
	And the last write time on bar\bar is 5/21/1970
	
Scenario: Setting UTC last write time
	Given a clean test directory
	When I set UTC last write time on bar\baz.txt to 5/21/1970
	And I set UTC last write time on bar\bar to 5/21/1970
	Then the UTC last write time on bar\baz.txt is 5/21/1970
	And the UTC last write time on bar\bar is 5/21/1970
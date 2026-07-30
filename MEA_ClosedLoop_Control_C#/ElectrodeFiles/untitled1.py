# -*- coding: utf-8 -*-
"""
Created on Fri Apr 26 11:46:51 2024

@author: Domingos
"""

# Open the input and output files
input_file = '252_9well_electrode_labels.txt'
output_file = '252_9well_electrode_labels___.txt'

# Open the input file for reading
with open(input_file, 'r') as infile:
    # Read all lines from the input file
    lines = infile.readlines()

# Open the output file for writing
with open(output_file, 'w') as outfile:
    # Write each line with ' added to the beginning and end
    for line in lines:
        # Strip any leading/trailing whitespace from the line
        line = line.strip()
        # Add ' to the beginning and end of the line
        modified_line = "'" + line + "'\n"
        # Write the modified line to the output file
        outfile.write(modified_line)

print("Lines modified and written to", output_file)
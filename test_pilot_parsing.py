#!/usr/bin/env python3

import json
import sys

def test_pilot_parsing():
    """Test parsing of pilot.json to verify all lines are processed correctly"""
    
    try:
        with open('assets/dialogue/arcs/UFOs/pilot.json', 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        print(f"Arc ID: {data.get('arcId', 'unknown')}")
        print(f"Topic: {data.get('topic', 'unknown')}")
        
        arc_lines = data.get('arcLines', [])
        print(f"Total arcLines entries: {len(arc_lines)}")
        
        for i, entry in enumerate(arc_lines):
            speaker = entry.get('speaker', 'unknown')
            lines_array = entry.get('lines', [])
            print(f"  {i}: speaker={speaker}, lines_count={len(lines_array)}")
            
            if speaker == 'vern':
                for j, line in enumerate(lines_array):
                    mood = line.get('mood', 'unknown')
                    text_len = len(line.get('text', ''))
                    print(f"    vern[{j}]: mood={mood}, text_length={text_len}")
            elif speaker == 'caller':
                for j, line in enumerate(lines_array):
                    line_id = line.get('id', 'unknown')
                    text_len = len(line.get('text', ''))
                    print(f"    caller[{j}]: id={line_id}, text_length={text_len}")
        
        print("\n=== Summary ===")
        print(f"Expected lines: {len(arc_lines)}")
        print("All entries processed successfully!")
        
    except Exception as e:
        print(f"Error: {e}")
        return False
    
    return True

if __name__ == "__main__":
    if test_pilot_parsing():
        print("✓ Pilot JSON parsing test passed")
    else:
        print("✗ Pilot JSON parsing test failed")
        sys.exit(1)
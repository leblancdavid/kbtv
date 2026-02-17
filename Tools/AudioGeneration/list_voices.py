#!/usr/bin/env python3
"""
Fetch and display available voices from ElevenLabs account
"""

import os
import sys
import requests

# Get API key
config_path = os.path.join(os.path.dirname(__file__), 'elevenlabs_config.json')
api_key = None

try:
    import json
    with open(config_path, 'r') as f:
        config = json.load(f)
        api_key = config.get('elevenlabs_api_key')
except:
    pass

if not api_key:
    api_key = os.getenv('ELEVENLABS_API_KEY')

if not api_key:
    print("ERROR: No API key found")
    exit(1)

# Fetch voices
url = "https://api.elevenlabs.io/v1/voices"
headers = {"xi-api-key": api_key}

response = requests.get(url, headers=headers)

if response.status_code == 200:
    data = response.json()
    voices = data.get('voices', [])
    
    print(f"Found {len(voices)} available voices:")
    print("=" * 70)
    
    for i, voice in enumerate(voices, 1):
        voice_id = voice.get('voice_id', 'N/A')
        name = voice.get('name', 'N/A')
        category = voice.get('category', 'N/A')
        
        # Get description if available
        description = voice.get('description', '')
        
        print(f"{i:2}. {name}")
        print(f"    ID: {voice_id}")
        print(f"    Category: {category}")
        if description:
            print(f"    Description: {description}")
        print()
    
    print("=" * 70)
    print("Copy the voice IDs you want to use for the caller voice pool")
    
else:
    print(f"Error: {response.status_code}")
    print(response.text)

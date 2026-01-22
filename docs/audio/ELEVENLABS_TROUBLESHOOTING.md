# Troubleshooting ElevenLabs Voice Cloning Permissions

## 🔍 Issue: API Key Missing Voice Cloning Permission

**Error:** `"missing_permissions": "create_instant_voice_clone"`

## 🛠️ Solutions:

### 1. **Regenerate API Key** (Most Likely Solution)
After upgrading your ElevenLabs membership:

1. Go to https://elevenlabs.io/app/profile
2. Scroll to **API Key** section
3. Click **"Regenerate API Key"**
4. Copy the new API key
5. Update the script with the new key

### 2. **Verify Subscription Status**
- Check your ElevenLabs account billing/subscription
- Ensure you have **Instant Voice Cloning** enabled
- Some plans may require additional setup

### 3. **Wait for Propagation**
- API key changes may take 5-10 minutes to propagate
- Try again in a few minutes

### 4. **Update Script with New Key**
Once you have the new API key, update this line in `elevenlabs_setup.py`:

```python
self.api_key = api_key or os.getenv('ELEVENLABS_API_KEY') or "YOUR_NEW_API_KEY_HERE"
```

## 🎯 Alternative: Use Web Interface

If API issues persist, you can:

1. Go to https://elevenlabs.io/app/voice-lab
2. Click **"Add Voice"** → **"Instant Voice Clone"**
3. Upload `vern_reference_001_final.wav`
4. Name it "Vern Tell - Art Bell Inspired"
5. Use the web interface to generate audio

## 📞 Contact ElevenLabs Support

If issues persist:
- Email: support@elevenlabs.io
- Include your subscription details
- Mention you're trying to use Instant Voice Cloning

---

**Most likely: You need to regenerate your API key after upgrading your membership!** 🔑
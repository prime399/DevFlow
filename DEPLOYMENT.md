# Deploying DevFlow to Netlify or Vercel

This guide explains how to deploy your Uno Platform WebAssembly application to Netlify or Vercel with automated CI/CD.

## Prerequisites

- Your code pushed to a GitHub repository
- A Netlify or Vercel account
- .NET 10 SDK installed locally (for testing)

## Option 1: Netlify Deployment

### Manual Deployment via Netlify CLI

1. **Install Netlify CLI:**
   ```powershell
   npm install -g netlify-cli
   ```

2. **Build your WASM app:**
   ```powershell
   dotnet publish DevFlow/DevFlow.csproj -c Release -f net10.0-browserwasm
   ```

3. **Login to Netlify:**
   ```powershell
   netlify login
   ```

4. **Deploy:**
   ```powershell
   netlify deploy --prod --dir=DevFlow/bin/Release/net10.0-browserwasm/publish/wwwroot
   ```

### Automated Deployment via Netlify UI

1. Go to [app.netlify.com](https://app.netlify.com)
2. Click "Add new site" → "Import an existing project"
3. Connect your GitHub repository
4. Use these build settings:
   - **Build command:** `dotnet publish DevFlow/DevFlow.csproj -c Release -f net10.0-browserwasm`
   - **Publish directory:** `DevFlow/bin/Release/net10.0-browserwasm/publish/wwwroot`
5. Click "Deploy site"

### CI/CD with GitHub Actions

1. **Get your Netlify credentials:**
   - Go to Netlify → User Settings → Applications → Personal access tokens
   - Create a new token and copy it
   - Go to your site → Site settings → General → Site details
   - Copy your Site ID

2. **Add secrets to GitHub:**
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Add these secrets:
     - `NETLIFY_AUTH_TOKEN`: Your Netlify personal access token
     - `NETLIFY_SITE_ID`: Your site ID

3. **Push changes:**
   The workflow file `.github/workflows/deploy-netlify.yml` is already configured. Push your code to trigger deployment.

---

## Option 2: Vercel Deployment

### Manual Deployment via Vercel CLI

1. **Install Vercel CLI:**
   ```powershell
   npm install -g vercel
   ```

2. **Build your WASM app:**
   ```powershell
   dotnet publish DevFlow/DevFlow.csproj -c Release -f net10.0-browserwasm
   ```

3. **Login to Vercel:**
   ```powershell
   vercel login
   ```

4. **Deploy:**
   ```powershell
   cd DevFlow/bin/Release/net10.0-browserwasm/publish/wwwroot
   vercel --prod
   ```

### Automated Deployment via Vercel UI

1. Go to [vercel.com](https://vercel.com)
2. Click "Add New..." → "Project"
3. Import your GitHub repository
4. Configure:
   - **Framework Preset:** Other
   - **Build Command:** `dotnet publish DevFlow/DevFlow.csproj -c Release -f net10.0-browserwasm`
   - **Output Directory:** `DevFlow/bin/Release/net10.0-browserwasm/publish/wwwroot`
5. Click "Deploy"

### CI/CD with GitHub Actions

1. **Get your Vercel credentials:**
   - Install Vercel CLI: `npm install -g vercel`
   - Run: `vercel login`
   - Run: `vercel link` in your project directory
   - This creates `.vercel/project.json` with your project details
   - Get your token from [Vercel → Settings → Tokens](https://vercel.com/account/tokens)

2. **Add secrets to GitHub:**
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Add these secrets:
     - `VERCEL_TOKEN`: Your Vercel token
     - `VERCEL_ORG_ID`: From `.vercel/project.json` (orgId field)
     - `VERCEL_PROJECT_ID`: From `.vercel/project.json` (projectId field)

3. **Push changes:**
   The workflow file `.github/workflows/deploy-vercel.yml` is already configured. Push your code to trigger deployment.

---

## Configuration Files Created

- **`netlify.toml`**: Netlify configuration for build and routing
- **`vercel.json`**: Vercel configuration for build and routing
- **`.github/workflows/deploy-netlify.yml`**: GitHub Actions workflow for Netlify
- **`.github/workflows/deploy-vercel.yml`**: GitHub Actions workflow for Vercel

---

## Testing Locally

Before deploying, test your WASM build locally:

```powershell
# Build
dotnet publish DevFlow/DevFlow.csproj -c Release -f net10.0-browserwasm

# Serve locally (requires a web server)
# Option 1: Using dotnet serve
dotnet tool install --global dotnet-serve
dotnet serve -d DevFlow/bin/Release/net10.0-browserwasm/publish/wwwroot -p 8080

# Option 2: Using Python
cd DevFlow/bin/Release/net10.0-browserwasm/publish/wwwroot
python -m http.server 8080
```

Then open http://localhost:8080 in your browser.

---

## Troubleshooting

### Build Fails
- Ensure .NET 10 SDK is installed
- Check that all dependencies are restored: `dotnet restore`
- Verify the target framework is correct: `net10.0-browserwasm`

### WASM Files Don't Load
- Check that proper MIME types are set (configured in `netlify.toml` and `vercel.json`)
- Verify the publish directory path is correct
- Check browser console for CORS or Content-Type errors

### Routing Issues (404 on refresh)
- Ensure the SPA redirect rules are in place (already configured)
- For Netlify: Check `netlify.toml` redirects
- For Vercel: Check `vercel.json` routes

### Large Bundle Size
- Consider enabling [Uno Platform's Linking](https://platform.uno/docs/articles/features/using-il-linker-uno-ui.html)
- Enable compression in your hosting platform
- Both Netlify and Vercel automatically compress assets

---

## Next Steps

1. Choose either Netlify or Vercel (both work great!)
2. Set up the required secrets in GitHub
3. Push your code to trigger the first deployment
4. Your app will automatically deploy on every push to master/main

## Additional Resources

- [Uno Platform WebAssembly Documentation](https://platform.uno/docs/articles/features/using-wasm.html)
- [Netlify Documentation](https://docs.netlify.com/)
- [Vercel Documentation](https://vercel.com/docs)

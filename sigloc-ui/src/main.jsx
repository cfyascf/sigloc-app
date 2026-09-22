import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import { BrowserRouter } from "react-router-dom"
import { GoogleOAuthProvider } from "@react-oauth/google"

import "./index.css"
import App from "./App.jsx"
import { ThemeProvider } from "@/components/theme-provider"
import { AuthProvider } from "./contexts/AuthContext"
import { BrandProvider } from "./contexts/BrandContext"
import { GOOGLE_CLIENT_ID, isGoogleAuthEnabled } from "@/config/env"

// Only mount the Google provider when a Client ID is configured; otherwise the
// auth buttons degrade gracefully to a disabled placeholder.
function withGoogle(children) {
  if (!isGoogleAuthEnabled) {
    return children
  }

  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      {children}
    </GoogleOAuthProvider>
  )
}

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <ThemeProvider defaultTheme="light" storageKey="vite-ui-theme">
      <AuthProvider>
        <BrandProvider>
          <BrowserRouter>
            {withGoogle(<App />)}
          </BrowserRouter>
        </BrandProvider>
      </AuthProvider>
    </ThemeProvider>
  </StrictMode>
)
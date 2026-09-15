import { createApp } from 'vue'

import App from './App.vue'
import './assets/main.css'
import { loadAppConfig } from '@/config/appConfig'
import { createServices, installServices } from '@/core/container'
import { createAppRouter } from '@/router'

// Composition root: configuration is read once, services are built from it, and both
// are handed to the app. Nothing below this line constructs its own dependencies.
const config = loadAppConfig()
const services = createServices(config)

const app = createApp(App)

installServices(app, services)
app.use(createAppRouter(config.ui.appTitle))
app.mount('#app')

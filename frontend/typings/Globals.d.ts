declare module '*.module.css';

interface Window {
  Prowlarr: {
    apiKey: string;
    instanceName: string;
    theme: string;
    urlBase: string;
    version: string;
  };
}

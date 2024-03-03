export default function getPathWithUrlBase(path: string) {
  return `${window.Prowlarr.urlBase}${path}`;
}

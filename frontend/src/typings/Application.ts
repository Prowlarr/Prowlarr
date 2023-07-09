import ModelBase from 'App/ModelBase';

export enum ApplicationSyncLevel {
  Disabled = 'disabled',
  AddOnly = 'addOnly',
  FullSync = 'fullSync',
}

interface Application extends ModelBase {
  name: string;
  syncLevel: ApplicationSyncLevel;
  implementationName: string;
  implementation: string;
  configContract: string;
  infoLink: string;
  tags: number[];
}

export default Application;

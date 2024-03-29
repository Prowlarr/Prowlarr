import ModelBase from 'App/ModelBase';

export interface Field {
  order: number;
  name: string;
  label: string;
  value: boolean | number | string;
  type: string;
  advanced: boolean;
  privacy: string;
}

interface Notification extends ModelBase {
  enable: boolean;
  name: string;
  onGrab: boolean;
  onHealthIssue: boolean;
  onHealthRestored: boolean;
  includeHealthWarnings: boolean;
  onApplicationUpdate: boolean;
  supportsOnGrab: boolean;
  supportsOnHealthIssue: boolean;
  supportsOnHealthRestored: boolean;
  supportsOnApplicationUpdate: boolean;
  fields: Field[];
  implementationName: string;
  implementation: string;
  configContract: string;
  infoLink: string;
  tags: number[];
}

export default Notification;

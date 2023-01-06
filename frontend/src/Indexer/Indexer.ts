import ModelBase from 'App/ModelBase';

export interface IndexerStatus extends ModelBase {
  disabledTill: Date;
}

export interface IndexerCategory extends ModelBase {
  id: number;
  name: string;
  subCategories: IndexerCategory[];
}

export interface IndexerCapabilities extends ModelBase {
  limitsMax: number;
  limitsDefault: number;
  categories: IndexerCategory[];
}

export interface IndexerField extends ModelBase {
  name: string;
  label: string;
  advanced: boolean;
  type: string;
  value: string;
}

interface Indexer extends ModelBase {
  name: string;
  added: Date;
  enable: boolean;
  redirect: boolean;
  protocol: string;
  privacy: string;
  priority: number;
  fields: IndexerField[];
  tags: number[];
  status: IndexerStatus;
  capabilities: IndexerCapabilities;
  indexerUrls: string[];
}

export default Indexer;

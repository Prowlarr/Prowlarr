import ModelBase from 'App/ModelBase';

export interface IndexerStatus extends ModelBase {
  disabledTill: Date;
  initialFailure: Date;
  mostRecentFailure: Date;
}

export interface IndexerCategory extends ModelBase {
  id: number;
  name: string;
  subCategories: IndexerCategory[];
}

export interface IndexerCapabilities extends ModelBase {
  limitsMax: number;
  limitsDefault: number;
  supportsRawSearch: boolean;
  searchParams: string[];
  tvSearchParams: string[];
  movieSearchParams: string[];
  musicSearchParams: string[];
  bookSearchParams: string[];
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
  description: string;
  encoding: string;
  language: string;
  added: Date;
  enable: boolean;
  redirect: boolean;
  protocol: string;
  privacy: string;
  priority: number;
  fields: IndexerField[];
  tags: number[];
  sortName: string;
  status: IndexerStatus;
  capabilities: IndexerCapabilities;
  indexerUrls: string[];
}

export default Indexer;

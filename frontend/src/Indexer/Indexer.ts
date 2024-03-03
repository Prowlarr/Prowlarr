import ModelBase from 'App/ModelBase';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';

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

export type IndexerPrivacy = 'public' | 'semiPrivate' | 'private';

export interface IndexerField extends ModelBase {
  order: number;
  name: string;
  label: string;
  advanced: boolean;
  type: string;
  value: string;
  privacy: string;
}

interface Indexer extends ModelBase {
  name: string;
  definitionName: string;
  description: string;
  encoding: string;
  language: string;
  added: Date;
  enable: boolean;
  redirect: boolean;
  supportsRss: boolean;
  supportsSearch: boolean;
  supportsRedirect: boolean;
  supportsPagination: boolean;
  protocol: DownloadProtocol;
  privacy: IndexerPrivacy;
  priority: number;
  fields: IndexerField[];
  tags: number[];
  sortName: string;
  status: IndexerStatus;
  capabilities: IndexerCapabilities;
  indexerUrls: string[];
  legacyUrls: string[];
  appProfileId: number;
  implementationName: string;
  implementation: string;
  configContract: string;
  infoLink: string;
}

export default Indexer;

import ModelBase from 'App/ModelBase';
import { IndexerCategory } from 'Indexer/Indexer';

interface Release extends ModelBase {
  guid: string;
  categories: IndexerCategory[];
  protocol: string;
  title: string;
  sortTitle: string;
  fileName: string;
  infoUrl: string;
  downloadUrl?: string;
  magnetUrl?: string;
  indexerId: number;
  indexer: string;
  age: number;
  ageHours: number;
  ageMinutes: number;
  publishDate: string;
  size?: number;
  files?: number;
  grabs?: number;
  seeders?: number;
  leechers?: number;
  indexerFlags: string[];
}

export default Release;

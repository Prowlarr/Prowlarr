import React from 'react';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import { kinds, sizes } from 'Helpers/Props';
import { IndexerCategory } from 'Indexer/Indexer';
import styles from './ReleaseLinks.css';

interface ReleaseLinksProps {
  categories: IndexerCategory[];
  imdbId?: string;
  tmdbId?: number;
  tvdbId?: number;
  tvMazeId?: number;
}

function ReleaseLinks(props: ReleaseLinksProps) {
  const { categories = [], imdbId, tmdbId, tvdbId, tvMazeId } = props;

  const categoryNames = categories
    .filter((item) => item.id < 100000)
    .map((c) => c.name);

  return (
    <div className={styles.links}>
      {imdbId ? (
        <Link
          className={styles.link}
          to={`https://imdb.com/title/tt${imdbId.toString().padStart(7, '0')}/`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.INFO}
            size={sizes.LARGE}
          >
            IMDb
          </Label>
        </Link>
      ) : null}

      {tmdbId ? (
        <Link
          className={styles.link}
          to={`https://www.themoviedb.org/${
            categoryNames.includes('Movies') ? 'movie' : 'tv'
          }/${tmdbId}`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.INFO}
            size={sizes.LARGE}
          >
            TMDb
          </Label>
        </Link>
      ) : null}

      {tvdbId ? (
        <Link
          className={styles.link}
          to={`https://www.thetvdb.com/?tab=series&id=${tvdbId}`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.INFO}
            size={sizes.LARGE}
          >
            TVDb
          </Label>
        </Link>
      ) : null}

      {tvMazeId ? (
        <Link
          className={styles.link}
          to={`https://www.tvmaze.com/shows/${tvMazeId}/_`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.INFO}
            size={sizes.LARGE}
          >
            TV Maze
          </Label>
        </Link>
      ) : null}
    </div>
  );
}

export default ReleaseLinks;

import React, { useCallback, useState } from 'react';
import Link from 'Components/Link/Link';
import IndexerInfoModal from './Info/IndexerInfoModal';
import styles from './IndexerTitleLink.css';

interface IndexerTitleLinkProps {
  indexerId: number;
  title: string;
  onCloneIndexerPress(id: number): void;
}

function IndexerTitleLink(props: IndexerTitleLinkProps) {
  const { title, indexerId, onCloneIndexerPress } = props;

  const [isIndexerInfoModalOpen, setIsIndexerInfoModalOpen] = useState(false);

  const onIndexerInfoPress = useCallback(() => {
    setIsIndexerInfoModalOpen(true);
  }, [setIsIndexerInfoModalOpen]);

  const onIndexerInfoModalClose = useCallback(() => {
    setIsIndexerInfoModalOpen(false);
  }, [setIsIndexerInfoModalOpen]);

  return (
    <div>
      <Link className={styles.link} onPress={onIndexerInfoPress}>
        {title}
      </Link>

      <IndexerInfoModal
        indexerId={indexerId}
        isOpen={isIndexerInfoModalOpen}
        onModalClose={onIndexerInfoModalClose}
        onCloneIndexerPress={onCloneIndexerPress}
      />
    </div>
  );
}

export default IndexerTitleLink;
